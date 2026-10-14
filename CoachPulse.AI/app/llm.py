from __future__ import annotations

import json
import os
from abc import ABC, abstractmethod
from urllib.request import Request, urlopen

from .schemas import GenerateProgramRequest


class LLMClient(ABC):
    @property
    @abstractmethod
    def available(self) -> bool: ...

    @abstractmethod
    def complete(self, system_prompt: str, user_data: dict) -> str: ...


class FakeLLMClient(LLMClient):
    @property
    def available(self) -> bool:
        return True

    def complete(self, system_prompt: str, user_data: dict) -> str:
        allowed_fields = set(GenerateProgramRequest.model_fields)
        req = GenerateProgramRequest.model_validate({key: value for key, value in user_data.items() if key in allowed_fields})
        exercise = req.availableExercises[0]
        weeks = []
        for week in range(1, req.durationWeeks + 1):
            days = []
            for index in range(req.daysPerWeek):
                days.append({"day": ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"][index], "focus": req.goal, "exercises": [{"exerciseId": str(exercise.id), "name": exercise.name, "sets": 3, "reps": "10-12", "restSeconds": 90, "notes": ""}]})
            weeks.append({"week": week, "days": days})
        calories = req.caloriesTarget or 2000
        content = {
            "workoutPlan": {"weeks": weeks},
            "mealPlan": {"dailyCalories": calories, "macros": {"proteinG": 150, "carbsG": 200, "fatG": 65}, "days": [{"day": "Sunday", "meals": [{"name": "Breakfast", "items": "Oats and fruit", "calories": min(500, calories)}]}]},
            "unmatchedExercises": [],
        }
        return json.dumps(content)


class OpenAILLMClient(LLMClient):
    @property
    def available(self) -> bool:
        return bool(os.getenv("OPENAI_API_KEY"))

    def complete(self, system_prompt: str, user_data: dict) -> str:
        key = os.getenv("OPENAI_API_KEY")
        if not key:
            raise RuntimeError("OPENAI_API_KEY is not configured")
        body = json.dumps({"model": os.getenv("OPENAI_MODEL", "gpt-4o-mini"), "temperature": 0.2, "response_format": {"type": "json_object"}, "messages": [{"role": "system", "content": system_prompt}, {"role": "user", "content": json.dumps(user_data, ensure_ascii=False)}]}).encode()
        request = Request(os.getenv("OPENAI_BASE_URL", "https://api.openai.com/v1/chat/completions"), data=body, headers={"Authorization": f"Bearer {key}", "Content-Type": "application/json"}, method="POST")
        with urlopen(request, timeout=50) as response:
            result = json.loads(response.read())
        return result["choices"][0]["message"]["content"]


def make_llm_client() -> LLMClient:
    if os.getenv("LLM_PROVIDER", "mock").lower() == "openai":
        return OpenAILLMClient()
    return FakeLLMClient()
