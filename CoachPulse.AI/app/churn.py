from __future__ import annotations

import json
import os
from pathlib import Path

import joblib
import numpy as np
from pydantic import BaseModel

from .schemas import ChurnFeatures, ChurnModelInfo, ChurnResponse

FEATURES = ["checkInsLast14d", "checkInRateLast30d", "workoutCompletionRate14d", "avgReplyTimeHours", "daysSinceLastLogin", "daysToSubscriptionEnd", "messagesSentLast14d", "subscriptionsRenewedCount", "hasPendingPayment"]
MODEL_PATH = Path(os.getenv("CHURN_MODEL_PATH", Path(__file__).parent.parent / "models" / "churn.joblib"))
INFO_PATH = MODEL_PATH.with_suffix(".json")


class ChurnModel:
    def __init__(self) -> None:
        self.model = None
        self.info: dict | None = None

    def load(self, model_path: Path = MODEL_PATH) -> bool:
        info_path = model_path.with_suffix(".json")
        if not model_path.exists() or not info_path.exists():
            return False
        self.model = joblib.load(model_path)
        self.info = json.loads(info_path.read_text(encoding="utf-8"))
        return True

    def predict(self, client_id, features: ChurnFeatures) -> ChurnResponse:
        if self.model is None or self.info is None:
            raise RuntimeError("Churn model is not loaded")
        values = features.model_dump()
        row = [float(values[name]) for name in FEATURES]
        score = float(self.model.predict_proba([row])[0][1])
        low = float(os.getenv("CHURN_LOW_THRESHOLD", "0.4"))
        high = float(os.getenv("CHURN_HIGH_THRESHOLD", "0.7"))
        level = "Low" if score < low else "Medium" if score <= high else "High"
        importance = self.model.feature_importances_
        ranked = sorted(zip(FEATURES, importance, row), key=lambda item: float(item[1]) * abs(float(item[2])), reverse=True)[:3]
        top = [{"feature": name, "impact": float(imp) * abs(float(value))} for name, imp, value in ranked]
        return ChurnResponse(clientId=client_id, riskScore=score, riskLevel=level, topFactors=top, modelVersion=self.info["modelVersion"])

    def get_info(self) -> dict:
        if self.info is None:
            raise RuntimeError("Churn model is not loaded")
        return self.info
