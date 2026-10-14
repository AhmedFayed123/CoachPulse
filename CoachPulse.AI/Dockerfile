FROM python:3.12.10-slim
ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1
WORKDIR /srv/coachpulse-ai
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY app ./app
COPY train.py ./train.py
RUN adduser --system --group --no-create-home coachpulse && python train.py && chown -R coachpulse:coachpulse /srv/coachpulse-ai
EXPOSE 8000
USER coachpulse
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
