FROM python:3.12-slim
ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1
WORKDIR /srv/coachpulse-ai
COPY requirements.txt .
RUN pip install --default-timeout=1000 --retries 5 --no-cache-dir -r requirements.txt
COPY app ./app
COPY train.py ./train.py
RUN adduser --system --group --no-create-home coachpulse && python train.py && chown -R coachpulse:coachpulse /srv/coachpulse-ai
EXPOSE 8000
USER coachpulse
HEALTHCHECK --interval=10s --timeout=3s --start-period=30s --retries=5 CMD python -c "import urllib.request; urllib.request.urlopen('http://127.0.0.1:8000/health', timeout=2)"
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
