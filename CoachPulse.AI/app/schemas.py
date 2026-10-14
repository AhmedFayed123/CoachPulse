from __future__ import annotations

from typing import Literal
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field, field_validator


class AvailableExercise(BaseModel):
    id: UUID
    name: str = Field(min_length=1, max_length=150)
    muscleGroup: str = Field(min_length=1, max_length=80)
    equipment: str | None = Field(default=None, max_length=120)


class GenerateProgramRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")
    clientId: UUID
    goal: Literal["FatLoss", "MuscleGain", "Strength", "GeneralFitness", "Rehabilitation"]
    level: Literal["Beginner", "Intermediate", "Advanced"]
    daysPerWeek: int = Field(ge=1, le=7)
    sessionMinutes: int = Field(ge=15, le=240)
    durationWeeks: int = Field(ge=1, le=24)
    equipment: list[str] = Field(default_factory=list, max_length=30)
    injuries: list[str] = Field(default_factory=list, max_length=30)
    dietaryRestrictions: list[str] = Field(default_factory=list, max_length=30)
    caloriesTarget: int | None = Field(default=None, ge=800, le=10000)
    language: Literal["ar", "en"]
    extraNotes: str = Field(default="", max_length=2000)
    availableExercises: list[AvailableExercise] = Field(min_length=1, max_length=1000)


class WorkoutExercise(BaseModel):
    exerciseId: UUID
    name: str = Field(min_length=1, max_length=150)
    sets: int = Field(ge=1, le=20)
    reps: str = Field(min_length=1, max_length=30)
    restSeconds: int = Field(ge=0, le=600)
    notes: str = Field(default="", max_length=500)


class WorkoutDay(BaseModel):
    day: str = Field(min_length=1, max_length=30)
    focus: str = Field(min_length=1, max_length=100)
    exercises: list[WorkoutExercise] = Field(max_length=40)


class WorkoutWeek(BaseModel):
    week: int = Field(ge=1, le=24)
    days: list[WorkoutDay] = Field(min_length=1, max_length=7)


class WorkoutPlan(BaseModel):
    weeks: list[WorkoutWeek] = Field(min_length=1, max_length=24)


class Macros(BaseModel):
    proteinG: float = Field(ge=0, le=1000)
    carbsG: float = Field(ge=0, le=1500)
    fatG: float = Field(ge=0, le=1000)


class Meal(BaseModel):
    name: str = Field(min_length=1, max_length=100)
    items: str = Field(min_length=1, max_length=1000)
    calories: int = Field(ge=0, le=5000)


class MealDay(BaseModel):
    day: str = Field(min_length=1, max_length=30)
    meals: list[Meal] = Field(max_length=12)


class MealPlan(BaseModel):
    dailyCalories: int = Field(ge=800, le=10000)
    macros: Macros
    days: list[MealDay] = Field(min_length=1, max_length=7)


class GeneratedProgramContent(BaseModel):
    workoutPlan: WorkoutPlan
    mealPlan: MealPlan
    unmatchedExercises: list[str] = Field(default_factory=list, max_length=200)


class ChurnFeatures(BaseModel):
    checkInsLast14d: int = Field(ge=0, le=100)
    checkInRateLast30d: float = Field(ge=0, le=1)
    workoutCompletionRate14d: float = Field(ge=0, le=1)
    avgReplyTimeHours: float = Field(ge=0, le=10000)
    daysSinceLastLogin: int = Field(ge=0, le=10000)
    daysToSubscriptionEnd: int = Field(ge=-10000, le=10000)
    messagesSentLast14d: int = Field(ge=0, le=10000)
    subscriptionsRenewedCount: int = Field(ge=0, le=1000)
    hasPendingPayment: bool


class ChurnRequest(BaseModel):
    clientId: UUID
    features: ChurnFeatures


class ChurnFactor(BaseModel):
    feature: str
    impact: float


class ChurnResponse(BaseModel):
    clientId: UUID
    riskScore: float = Field(ge=0, le=1)
    riskLevel: Literal["Low", "Medium", "High"]
    topFactors: list[ChurnFactor]
    modelVersion: str


class ChurnBatchRequest(BaseModel):
    items: list[ChurnRequest] = Field(min_length=1, max_length=500)


class ChurnBatchResponse(BaseModel):
    items: list[ChurnResponse]


class ChurnModelInfo(BaseModel):
    modelVersion: str
    accuracy: float
    precision: float
    recall: float
    trainingData: str


class RegenerateProgramRequest(GenerateProgramRequest):
    originalProgramId: UUID
    coachNotes: str = Field(min_length=1, max_length=2000)
