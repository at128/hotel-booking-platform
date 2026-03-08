from typing import List, Union, Optional

from fastapi import FastAPI
from pydantic import BaseModel
from sentence_transformers import SentenceTransformer

app = FastAPI(title="embedding-service")

MODEL_NAME = "sentence-transformers/all-MiniLM-L6-v2"
model = SentenceTransformer(MODEL_NAME)


class EmbeddingRequest(BaseModel):
    input: Union[str, List[str]]
    model: Optional[str] = None


class EmbeddingItem(BaseModel):
    index: int
    embedding: List[float]


class EmbeddingResponse(BaseModel):
    data: List[EmbeddingItem]


@app.get("/health")
def health():
    return {"status": "ok", "model": MODEL_NAME}


@app.post("/embeddings", response_model=EmbeddingResponse)
def embeddings(request: EmbeddingRequest):
    inputs = request.input if isinstance(request.input, list) else [request.input]

    vectors = model.encode(
        inputs,
        normalize_embeddings=True,
        convert_to_numpy=True
    )

    data = [
        EmbeddingItem(index=i, embedding=vectors[i].tolist())
        for i in range(len(inputs))
    ]

    return EmbeddingResponse(data=data)