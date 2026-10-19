import json
from uuid import uuid4

from fastapi.testclient import TestClient

from app.churn import ChurnModel
from app.llm import FakeLLMClient
from app.main import create_app
from app.schemas import GenerateProgramRequest
from train import train_model

KEY = "test-internal-key"


def sample_request(exercise_id=None):
    return {"clientId": str(uuid4()), "goal": "FatLoss", "level": "Beginner", "daysPerWeek": 1, "sessionMinutes": 45, "durationWeeks": 1, "equipment": ["Dumbbells"], "injuries": [], "dietaryRestrictions": [], "caloriesTarget": 2000, "language": "en", "extraNotes": "", "availableExercises": [{"id": str(exercise_id or uuid4()), "name": "Press", "muscleGroup": "Chest", "equipment": "Dumbbells"}]}


def test_internal_endpoints_reject_wrong_key(monkeypatch):
    monkeypatch.setenv("INTERNAL_API_KEY", KEY)
    client = TestClient(create_app())
    assert client.get("/health").status_code == 200
    assert client.get("/ready").status_code == 401
    assert client.post("/generate-program", json=sample_request(), headers={"X-Internal-Api-Key": "wrong"}).status_code == 401


def test_generate_validates_ranges(monkeypatch):
    monkeypatch.setenv("INTERNAL_API_KEY", KEY)
    request = sample_request()
    request["daysPerWeek"] = 8
    response = TestClient(create_app()).post("/generate-program", json=request, headers={"X-Internal-Api-Key": KEY})
    assert response.status_code == 422


def test_unknown_exercise_goes_to_unmatched(monkeypatch):
    monkeypatch.setenv("INTERNAL_API_KEY", KEY)
    class UnknownExerciseLLM(FakeLLMClient):
        def complete(self, system_prompt, user_data):
            data = GenerateProgramRequest.model_validate({k: v for k, v in user_data.items() if k in GenerateProgramRequest.model_fields})
            return json.dumps({"workoutPlan": {"weeks": [{"week": 1, "days": [{"day": "Sunday", "focus": "Upper", "exercises": [{"exerciseId": str(uuid4()), "name": "Invented", "sets": 3, "reps": "8", "restSeconds": 60}]}]}]}, "mealPlan": {"dailyCalories": 2000, "macros": {"proteinG": 100, "carbsG": 200, "fatG": 60}, "days": [{"day": "Sunday", "meals": [{"name": "Breakfast", "items": "Oats", "calories": 400}]}]}, "unmatchedExercises": []})
    response = TestClient(create_app(llm_client=UnknownExerciseLLM())).post("/generate-program", json=sample_request(), headers={"X-Internal-Api-Key": KEY})
    assert response.status_code == 200
    assert response.json()["workoutPlan"]["weeks"][0]["days"][0]["exercises"] == []
    assert response.json()["unmatchedExercises"] == ["Invented"]


def test_malformed_llm_json_retries_twice_then_502(monkeypatch):
    monkeypatch.setenv("INTERNAL_API_KEY", KEY)
    class BrokenLLM(FakeLLMClient):
        calls = 0
        def complete(self, system_prompt, user_data):
            self.calls += 1
            return "{broken json"
    llm = BrokenLLM()
    response = TestClient(create_app(llm_client=llm)).post("/generate-program", json=sample_request(), headers={"X-Internal-Api-Key": KEY})
    assert response.status_code == 502
    assert llm.calls == 3


def test_churn_single_and_batch(tmp_path, monkeypatch):
    monkeypatch.setenv("INTERNAL_API_KEY", KEY)
    model_path = tmp_path / "churn.joblib"
    train_model(model_path)
    churn = ChurnModel()
    assert churn.load(model_path)
    client = TestClient(create_app(churn_model=churn))
    features = {"checkInsLast14d": 1, "checkInRateLast30d": 0.4, "workoutCompletionRate14d": 0.35, "avgReplyTimeHours": 30, "daysSinceLastLogin": 9, "daysToSubscriptionEnd": 6, "messagesSentLast14d": 2, "subscriptionsRenewedCount": 1, "hasPendingPayment": False}
    item = {"clientId": str(uuid4()), "features": features}
    headers = {"X-Internal-Api-Key": KEY}
    single = client.post("/predict-churn", json=item, headers=headers)
    batch = client.post("/predict-churn/batch", json={"items": [item, item]}, headers=headers)
    assert single.status_code == 200 and 0 <= single.json()["riskScore"] <= 1
    assert batch.status_code == 200 and len(batch.json()["items"]) == 2


def test_progress_summary_auth_validation_and_success(monkeypatch):
    monkeypatch.setenv("INTERNAL_API_KEY", KEY)
    client = TestClient(create_app(llm_client=FakeLLMClient()))
    payload = {"clientId": str(uuid4()), "language": "en", "metrics": [{"name": "weightKg", "current": 80, "previous": 82, "unit": "kg"}]}
    assert client.post("/progress-summary", json=payload).status_code == 401
    response = client.post("/progress-summary", json=payload, headers={"X-Internal-Api-Key": KEY})
    assert response.status_code == 200
    assert response.json()["summary"]
    payload["language"] = "fr"
    assert client.post("/progress-summary", json=payload, headers={"X-Internal-Api-Key": KEY}).status_code == 422
