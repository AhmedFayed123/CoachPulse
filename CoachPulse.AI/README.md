# CoachPulse internal AI service

This FastAPI service is internal to the application network. It does not publish a host port. Every route except `/health` requires `X-Internal-Api-Key`; set `INTERNAL_API_KEY` in the runtime environment/secret store. Never put a live key in source control.

## Local run

```powershell
cd CoachPulse.AI
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
python train.py
$env:INTERNAL_API_KEY = "local-test-key"
$env:LLM_PROVIDER = "mock"
uvicorn app.main:app --reload --port 8001
```

The mock LLM is deterministic and works without an external provider key. Configure the same value as the API's `Ai__InternalApiKey` environment setting and point `Ai__BaseUrl` to `http://127.0.0.1:8001/`. For an OpenAI-compatible provider, set `LLM_PROVIDER=openai` and provide `OPENAI_API_KEY`; `OPENAI_BASE_URL` can point to a compatible private endpoint. Readiness is healthy only when a churn artifact is loaded and the configured LLM client is available.

## Docker

The Compose service uses the private `ai-private` network (`internal: true`) and has no `ports:` mapping. It builds the seeded Random Forest from synthetic examples. Configure `.env` from `.env.example` locally, then supply its private key through a secret manager in production. The checked-in training script labels the sample dataset as synthetic; its metrics are not production accuracy claims.

The model is a Random Forest trained by `train.py` on deterministic synthetic data. Retrain with properly governed and labeled production data before using model metrics for business decisions.
