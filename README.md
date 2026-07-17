# LippoLand SOAP + Instana — Production Imitation POC

## What this proves

LippoLand's backend is a monolith. One module calls another via internal SOAP over
localhost. From Instana's perspective those calls are **completely invisible** today.

This POC imitates the **exact pattern** from the real LippoLand production code
(screenshot: `CustomSpan.Create()` wrapping `[WebMethod]` bodies) and proves that
with the instrumentation added, every SOAP operation becomes individually observable.

---

## Production code vs this POC

| Real LippoLand (production) | This POC |
|---|---|
| `CustomSpan.Create()` — Instana NuGet SDK | `InstanaSpan.Create()` — agent REST API |
| `span.SetTag("soapAction", "BookingUnit")` | `span.SetTag("soap.action", "BookingUnit")` |
| `span.WrapAction(() => { DB calls... }, true)` | `span.WrapAction(() => { stub logic... })` |
| `[WebMethod]` ASMX on .NET Framework 4.x / Windows | `[OperationContract]` CoreWCF on .NET 8 / Linux |
| Caller: raw `HttpWebRequest` + span | Caller: `SoapHttpClient` + span |

The NuGet SDK (`Instana.ManagedTracing.Sdk`) only ships `net45` — it won't run on
.NET 8 / Linux. `InstanaSpan` calls the agent's local REST API on port `42699`
directly. This is IBM's documented approach for modern runtimes and produces
**identical results** in the Instana UI.

---

## What Instana sees per SOAP call (with instrumentation)

```
  curl /trigger/with
       │
       ▼
  SoapHttpClient.Call("BookingUnit")          ← CALLER SIDE
    │
    ├─► POST /com.instana.plugin.generic.trace  (open EXIT span)
    │     type:           EXIT
    │     name:           soap.call
    │     soap.action:    "http://tempuri.org/BookingUnit"
    │     soap.operation: "BookingUnit"
    │
    ├─► POST /soap  (actual SOAP HTTP call)
    │     SOAPAction: "http://tempuri.org/BookingUnit"
    │     X-INSTANA-T: <traceId>
    │     X-INSTANA-S: <spanId>
    │         │
    │         ▼
    │   OnlineBookingService.BookingUnit()     ← SERVICE SIDE
    │     InstanaSpan.Create("BookingUnit")
    │     ≈ CustomSpan.Create() in real LippoLand
    │
    │     ├─► POST /com.instana.plugin.generic.trace  (open ENTRY span)
    │     │     type:       ENTRY
    │     │     name:       soap.server
    │     │     soap.action: "BookingUnit"
    │     │
    │     └─► POST /com.instana.plugin.generic.trace  (close ENTRY span)
    │               duration: Xms
    │               soap.action:    "BookingUnit"
    │               soap.operation: "BookingUnit"
    │               soap.type:      "server"
    │
    └─► POST /com.instana.plugin.generic.trace  (close EXIT span)
              duration: Xms
              soap.action:    "http://tempuri.org/BookingUnit"
```

**2 spans per operation.** Both filterable in Instana Analytics by `soap.action`.

---

## Run the demo

### Step 1 — Kill any previous container and start fresh

```bash
docker kill $(docker ps -q) 2>/dev/null; docker rm $(docker ps -aq) 2>/dev/null

cd ~/Documents/lippo/lippo-instana-soap

docker run --rm --network host \
  -v "$(pwd):/src" -w /src \
  -e INSTANA_AGENT_URL=http://localhost:42699 \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet run --project LippoLand.Soap.Monolith
```

Wait for:
```
║  LippoLand Monolith Demo  —  running on :8090  ║
```

### Step 2 — Fire both scenarios (second terminal)

```bash
# WITH instrumentation — Instana sees 2 spans per operation
curl http://localhost:8090/trigger/with

# WITHOUT instrumentation — Instana sees nothing
curl http://localhost:8090/trigger/without
```

### Step 3 — Verify in Instana

| Where | What to look for |
|---|---|
| **Analytics → Calls** | Filter `soap.action = "http://tempuri.org/BookingUnit"` — spans appear only after `/trigger/with` |
| **Infrastructure → Services** | `LippoLand-OnlineBooking` service node |
| **Service Map** | Self-referencing arrow (monolith calling itself) |

---

## Run the tests (no agent needed)

```bash
cd ~/Documents/lippo/lippo-instana-soap

docker run --rm \
  -v "$(pwd):/src" -w /src \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet run --project LippoLand.Soap.Tests
```

Expected:
```
Running SoapEnvelopesTests...  PASS   (12 tests — XML structure, AuthHeader, escaping)
Running SoapHttpClientTests... PASS   (11 tests — exit span, SOAPAction header, propagation)
Running ServiceSpanTests...    PASS   (8 tests  — entry span, WrapAction, graceful degradation)
Results: 3 passed, 0 failed
```

---

## Project structure

```
LippoLand.Soap.sln
│
├── LippoLand.Soap.Monolith/         ← THE DEMO — run this
│   └── Program.cs                   ← Single process: SOAP server + /trigger/with + /trigger/without
│
├── LippoLand.Soap.Service/          ← SOAP server (also used by Monolith)
│   ├── IOnlineBookingService.cs     ← [ServiceContract] matching real WSDL
│   ├── OnlineBookingService.cs      ← [WebMethod] bodies wrapped with InstanaSpan.Create()
│   │                                   ← mirrors: CustomSpan.Create() in real LippoLand
│   └── InstanaSpan.cs               ← SERVER-SIDE span helper
│                                       ← mirrors: Instana.ManagedTracing.Sdk CustomSpan
│
├── LippoLand.Soap.Client/           ← SOAP caller (also used by Monolith)
│   ├── SoapHttpClient.cs            ← CALLER-SIDE span helper (exit span)
│   ├── SoapEnvelopes.cs             ← SOAP 1.1 XML builder
│   └── Program.cs                   ← Standalone: fires 5 operations
│
└── LippoLand.Soap.Tests/            ← Test suite (31 tests, no agent needed)
    ├── SoapEnvelopesTests.cs        ← 12 tests: XML, AuthHeader, escaping
    ├── SoapHttpClientTests.cs       ← 11 tests: exit span, SOAPAction header, propagation
    ├── ServiceSpanTests.cs          ← 8 tests:  entry span, WrapAction, graceful degradation
    ├── InstanaStub.cs               ← Intercepts agent REST calls in memory
    └── StubHttpListener.cs          ← In-process SOAP stub server
```

---

## Deploying to production LippoLand

1. Copy `InstanaSpan.cs` into the LippoLand solution
2. Wrap every `[WebMethod]` body exactly as shown in the screenshot:

```csharp
// BEFORE (what LippoLand has today — invisible to Instana)
[WebMethod]
public DataSet GetComponentDiagramatic(string projectcode, string clustercode)
{
    DataTable dt = clsDataUnit.GetDataUnitDiagramatic(projectcode, clustercode);
    ds.Tables.Add(dt);
    return ds;
}

// AFTER (with instrumentation — Instana sees each operation individually)
[WebMethod]
public DataSet GetComponentDiagramatic(string projectcode, string clustercode)
{
    using (var span = InstanaSpan.Create("GetComponentDiagramatic"))
    {
        span.SetTag("soap.action", "GetComponentDiagramatic");
        return span.WrapAction(() =>
        {
            DataTable dt = clsDataUnit.GetDataUnitDiagramatic(projectcode, clustercode);
            ds.Tables.Add(dt);
            return ds;
        });
    }
}
```

3. Ensure the Instana agent is running on the same server (`ss -tlnp | grep 42699`)
4. Verify in Instana Analytics: filter `soap.action` — each operation appears individually

---

## Real service facts (from live WSDL)

| Property | Value |
|---|---|
| URL | `https://connect.lippoland.id/InternalMobileAppsService/WS_OnlineBooking.asmx` |
| Total operations | 127 |
| SOAP version | 1.1 and 1.2 |
| Namespace | `http://tempuri.org/` |
| Auth | SOAP header `AuthHeader` (domainName, userName, password) on every operation |

---

## Instana agent setup (fresh VM)

```bash
# Install (get fresh keys from Instana UI → Settings → Agents)
curl -o setup_agent.sh https://setup.instana.io/agent \
  && chmod 700 setup_agent.sh \
  && sudo ./setup_agent.sh -a <AGENT_KEY> -d <DOWNLOAD_KEY> \
     -t dynamic -e ingress-<id>.instana.io -p 443 -s

# Verify agent is ready
sudo tail -f /opt/instana/agent/data/log/agent.log   # look for "Agent is ready."
ss -tlnp | grep 42699                                 # port open = healthy
```

### Option A — zero-code baseline

Add to `/opt/instana/agent/etc/instana/configuration.yaml`:
```yaml
com.instana.plugin.dotnet:
  extra-http-headers:
    - SOAPAction
```
Restart agent. `SOAPAction` becomes searchable on raw HTTP spans — no code change needed.
Limitation: one generic endpoint per URL, not per operation.

### Option B — SDK spans (this POC)

`InstanaSpan` / `SoapHttpClient` give each operation its own named span.
This is what enables per-operation filtering, the Service Map link, and full trace context propagation.
