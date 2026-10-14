"""Train on deterministic synthetic data only. Replace with approved, consented labels before production claims."""
from __future__ import annotations

import json
from pathlib import Path

import joblib
import numpy as np
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import accuracy_score, precision_score, recall_score
from sklearn.model_selection import train_test_split

from app.churn import FEATURES

OUTPUT = Path(__file__).parent / "models" / "churn.joblib"


def train_model(output: Path = OUTPUT) -> dict:
    rng = np.random.default_rng(20261014)
    count = 3000
    x = np.column_stack([
        rng.poisson(1.2, count), rng.beta(2, 3, count), rng.beta(2, 3, count), rng.gamma(2, 15, count),
        rng.integers(0, 45, count), rng.integers(-10, 60, count), rng.poisson(5, count), rng.poisson(2, count), rng.integers(0, 2, count),
    ]).astype(float)
    score = (0.12 * (x[:, 0] < 1) + 0.16 * (x[:, 1] < 0.3) + 0.2 * (x[:, 2] < 0.35) + 0.12 * (x[:, 3] > 30) + 0.16 * (x[:, 4] > 10) + 0.12 * (x[:, 5] < 7) + 0.05 * (x[:, 6] < 2) + 0.03 * (x[:, 7] == 0) + 0.1 * (x[:, 8] > 0))
    y = (score + rng.normal(0, 0.12, count) > 0.48).astype(int)
    x_train, x_test, y_train, y_test = train_test_split(x, y, test_size=0.25, random_state=37, stratify=y)
    model = RandomForestClassifier(n_estimators=250, max_depth=9, min_samples_leaf=4, class_weight="balanced", random_state=37, n_jobs=-1)
    model.fit(x_train, y_train)
    metrics = {"modelVersion": "rf-synthetic-20261014-v1", "accuracy": float(accuracy_score(y_test, model.predict(x_test))), "precision": float(precision_score(y_test, model.predict(x_test), zero_division=0)), "recall": float(recall_score(y_test, model.predict(x_test), zero_division=0)), "trainingData": "SYNTHETIC ONLY; 3000 seeded generated examples; not representative clinical/customer data."}
    output.parent.mkdir(parents=True, exist_ok=True)
    joblib.dump(model, output)
    output.with_suffix(".json").write_text(json.dumps(metrics, indent=2), encoding="utf-8")
    return metrics


if __name__ == "__main__":
    print(json.dumps(train_model(), indent=2))
