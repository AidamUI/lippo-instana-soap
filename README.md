# LippoLand SOAP + Instana — Monolith Observability POC

## The problem

LippoLand's entire backend is a **monolith calling itself via SOAP** (WS_OnlineBooking.asmx).
Every operation — `BookingUnit`, `retrieveComponentDiagramatic`, `ReserveSelectedUnit`, all
127 of them — hits the **same URL**:

```
POST https://connect.lippoland.id/InternalMobileAppsService/WS_OnlineBooking.asmx
```

Instana auto-instrumentation only sees that URL. It does not read the `SOAPAction` HTTP
header by default. So every operation looks identical — one anonymous `HTTP POST`, no
operation name, no way to filter, no dependency link in the Service Map.

**Hypothesis:** Instana detects the HTTP call but cannot differentiate which SOAP operation
it is — because the operation identity is in the `SOAPAction` header and the SOAP body,
not in the URL.

---

## The answer — confirmed live

**Yes, Instana CAN differentiate SOAP operations** — but only with instrumentation added.
Two approaches, both proven working:

### Option A — Zero code change (agent config only)

Add to `/opt/instana/agent/etc/instana/configuration.yaml`:

```yaml
com.instana.plugin.generic.http:
  extra-http-headers:
    - SOAPAction
```

```bash
sudo mkdir -p /opt/instana/agent/etc/instana
sudo tee /opt/instana/agent/etc/instana/configuration.yaml << 'EOF'
com.instana.plugin.generic.http:
  extra-http-headers:
    - SOAPAction
EOF
sudo systemctl restart instana-agent
```

After restart, the `SOAPAction` header is captured on every HTTP call automatically.
In Analytics → Calls → **HTTP → Call Http Header → Key: SOAPAction → Value: http://tempuri.org/BookingUnit**

**Limitation:** Still one generic HTTP endpoint per URL. You can filter by SOAPAction value
but all calls share the same endpoint name.

### Option B — SDK spans (this POC)

Wrap each `[WebMethod]` body with `InstanaSpan.Create()`. This gives every operation its
own **named endpoint** in Instana — individually visible in the Service view, Analytics,
and Service Map.

**What we proved live:**

```
LippoLand-OnlineBooking service → Top endpoints:
  retrieveAvailableUnitForOnlineBooking   75ms
  BookingUnit                             30ms
```

Each SOAP operation is individually named and measurable. That is the answer.

---

## How the SOAPAction works (from real LippoLand WSDL)

Every call to the real service must include `SOAPAction` as an HTTP header, double-quoted:

```
POST /InternalMobileAppsService/WS_OnlineBooking.asmx HTTP/1.1
Host: connect.lippoland.id
Content-Type: text/xml; charset=utf-8
SOAPAction: "http://tempuri.org/BookingUnit"

<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Header>
    <AuthHeader xmlns="http://tempuri.org/">
      <domainName>LIPPOLAND</domainName>
      <userName>user</userName>
      <password>pass</password>
    </AuthHeader>
  </soap:Header>
  <soap:Body>
    <BookingUnit xmlns="http://tempuri.org/">
      <JSON>{"unitCode":"U-2BR-012"}</JSON>
    </BookingUnit>
  </soap:Body>
</soap:Envelope>
```

The pattern is identical for all 127 operations — only the `SOAPAction` value and the
body element name change. Every operation takes a single `JSON` string parameter and
returns a single `string` (JSON response).

Our [`SoapEnvelopes.cs`](LippoLand.Soap.Client/SoapEnvelopes.cs) builds these envelopes.
Our [`SoapHttpClient.cs`](LippoLand.Soap.Client/SoapHttpClient.cs) sets the `SOAPAction`
header and reports it to Instana.

---

## Production code vs this POC

The real LippoLand dev has already started instrumenting `[WebMethod]` bodies with
`CustomSpan.Create()` from `Instana.ManagedTracing.Sdk`. This POC imitates that exactly.

| Real LippoLand (production) | This POC |
|---|---|
| `CustomSpan.Create()` — Instana NuGet SDK (`net45`) | `InstanaSpan.Create()` — agent REST API |
| `span.SetTag("soapAction", "BookingUnit")` | `span.SetTag("soap.action", "BookingUnit")` |
| `span.WrapAction(() => { DB calls... }, true)` | `span.WrapAction(() => { stub logic... })` |
| `[WebMethod]` ASMX on .NET Framework 4.x / Windows | `[OperationContract]` CoreWCF on .NET 8 / Linux |
| Caller: raw `HttpWebRequest` | Caller: `SoapHttpClient` |

The NuGet SDK only ships `net45` binaries — it won't run on .NET 8 / Linux.
`InstanaSpan` uses the agent's local REST API on port `42699` directly, which is IBM's
documented approach for modern runtimes and produces identical results in the UI.

**Key discovery:** The agent REST API requires tags in a `tags{}` top-level object (not
`data{}`). `tags{}` = `SetTag()` in the SDK = **searchable in Unbounded Analytics**.
`data{}` = `SetData()` = transported but not searchable.

---

## What Instana sees per SOAP call

```
  curl /trigger/with
       │
       ▼
  ┌─────────────────────────────────────────────────────────┐
  │  LippoLand Monolith (one process)                       │
  │                                                         │
  │  CALLER SIDE — SoapHttpClient                          │
  │    POST /com.instana.plugin.generic.trace  →  agent    │
  │    {                                                    │
  │      type:     "EXIT",                                  │
  │      name:     "soap.call",                             │
  │      duration: 312,                                     │
  │      error:    false,                                   │
  │      tags: {                                            │
  │        "soap.action":    "http://tempuri.org/BookingUnit"│
  │        "soap.operation": "BookingUnit"                  │
  │      },                                                 │
  │      data: {                                            │
  │        "service":  "LippoLand-OnlineBooking",           │
  │        "endpoint": "BookingUnit"                        │
  │      }                                                  │
  │    }                                                    │
  │                                                         │
  │    POST /soap  (actual SOAP HTTP call)                  │
  │      SOAPAction: "http://tempuri.org/BookingUnit"       │
  │      X-INSTANA-T: <traceId>                             │
  │      X-INSTANA-S: <spanId>                              │
  │         │                                               │
  │         ▼                                               │
  │  SERVICE SIDE — InstanaSpan / CustomSpan.Create()       │
  │    POST /com.instana.plugin.generic.trace  →  agent    │
  │    {                                                    │
  │      type:     "ENTRY",                                 │
  │      name:     "soap.server",                           │
  │      duration: 45,                                      │
  │      error:    false,                                   │
  │      tags: {                                            │
  │        "soap.action":    "BookingUnit",                 │
  │        "soap.operation": "BookingUnit",                 │
  │        "soap.type":      "server"                       │
  │      },                                                 │
  │      data: {                                            │
  │        "service":  "LippoLand-OnlineBooking",           │
  │        "endpoint": "BookingUnit"                        │
  │      }                                                  │
  │    }                                                    │
  └─────────────────────────────────────────────────────────┘

  curl /trigger/without
    plain HttpClient → POST /soap → nothing sent to agent
    Instana sees: nothing
```

---

## Run the demo

### Prerequisites

- Docker running (`docker ps` works without sudo)
- Instana agent running (`ss -tlnp | grep 42699` shows a port listening)
- Agent config has `extra-http-headers: [SOAPAction]` (see Option A above)

### Step 1 — Start the monolith

```bash
docker kill $(docker ps -q) 2>/dev/null

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

### Step 2 — Fire both scenarios

```bash
# WITH instrumentation — Instana sees named spans per operation
curl http://localhost:8090/trigger/with

# WITHOUT instrumentation — Instana sees nothing
curl http://localhost:8090/trigger/without
```

### Step 3 — Find results in Instana

**Service view (most reliable):**
Infrastructure → Services → click **LippoLand-OnlineBooking** → Endpoints tab
→ `BookingUnit` and `retrieveAvailableUnitForOnlineBooking` listed individually

**Analytics → Calls:**
Clear filters → set time to Last 15 minutes → you'll see:
- `soap.server` calls (SERVICE side — entry span)
- `Internal trigger` calls (CALLER side — exit span)

**HTTP Header filter (Option A):**
Analytics → Calls → Add filter → HTTP → Call Http Header
→ Key: `SOAPAction` → Value: `http://tempuri.org/BookingUnit`

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
├── LippoLand.Soap.Monolith/         ← THE DEMO — only project you run
│   ├── Program.cs                   ← Single process: SOAP server + caller
│   │                                   GET /trigger/with    → instrumented
│   │                                   GET /trigger/without → uninstrumented
│   └── LippoLand.Soap.Monolith.csproj  ← compiles in Service + Client source files
│
├── LippoLand.Soap.Service/          ← SOAP server source (compiled into Monolith)
│   ├── IOnlineBookingService.cs     ← [ServiceContract] — 15 ops from live WSDL
│   ├── OnlineBookingService.cs      ← [WebMethod] bodies wrapped with InstanaSpan.Create()
│   │                                   mirrors: CustomSpan.Create() in real LippoLand
│   └── InstanaSpan.cs               ← server-side entry span helper
│                                       mirrors: Instana.ManagedTracing.Sdk CustomSpan
│
├── LippoLand.Soap.Client/           ← SOAP caller source (compiled into Monolith)
│   ├── SoapHttpClient.cs            ← caller-side exit span — reports to agent REST API
│   ├── SoapEnvelopes.cs             ← SOAP 1.1 XML builder with AuthHeader
│   └── Program.cs                   ← standalone runner (fires 5 ops against any URL)
│
└── LippoLand.Soap.Tests/            ← 31 tests, no agent needed
    ├── SoapEnvelopesTests.cs        ← 12 tests: XML, AuthHeader, special char escaping
    ├── SoapHttpClientTests.cs       ← 11 tests: exit span JSON, SOAPAction header, trace propagation
    ├── ServiceSpanTests.cs          ←  8 tests: entry span, WrapAction, graceful degradation
    ├── InstanaStub.cs               ← intercepts agent REST calls in memory
    └── StubHttpListener.cs          ← in-process SOAP stub server for integration tests
```

---

## Deploying to production LippoLand

**Step 1 — Agent config (zero code change, do first)**

```bash
sudo mkdir -p /opt/instana/agent/etc/instana
sudo tee /opt/instana/agent/etc/instana/configuration.yaml << 'EOF'
com.instana.plugin.generic.http:
  extra-http-headers:
    - SOAPAction
EOF
sudo systemctl restart instana-agent
```

**Step 2 — Wrap each `[WebMethod]` (per-operation endpoint names)**

Copy `InstanaSpan.cs` into the LippoLand solution. Then wrap each `[WebMethod]`:

```csharp
// BEFORE — invisible to Instana
[WebMethod]
public DataSet GetComponentDiagramatic(string projectcode, string clustercode)
{
    DataTable dt = clsDataUnit.GetDataUnitDiagramatic(projectcode, clustercode);
    ds.Tables.Add(dt);
    return ds;
}

// AFTER — named endpoint visible in Instana
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

`InstanaSpan.Create()` never throws — if the agent is unreachable the SOAP call still
completes normally. It is safe to add to every `[WebMethod]` without risk.

---

## Real service facts (from live WSDL)

| Property | Value |
|---|---|
| URL | `https://connect.lippoland.id/InternalMobileAppsService/WS_OnlineBooking.asmx` |
| Total operations | 127 |
| SOAP version | 1.1 (SOAPAction header) and 1.2 (no SOAPAction, action in Content-Type) |
| Namespace | `http://tempuri.org/` |
| Auth | `<AuthHeader>` in SOAP Header — domainName, userName, password — on every operation |
| Parameter style | ~85 ops take a single `JSON` string; ~28 use named string params; ~8 use mixed types |
| Port 80 | Firewalled — HTTPS only (port 443) |
