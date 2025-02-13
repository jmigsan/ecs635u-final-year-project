# server.py (placed in your Assets/StreamingAssets folder)
from fastapi import FastAPI
from pydantic import BaseModel
import uvicorn

app = FastAPI()

class Query(BaseModel):
    query: str

def process_query(query: str) -> str:
    # Replace this with your actual LLM logic
    return f"Processed: {query}"

@app.post("/llm")
async def llm_endpoint(data: Query):
    response = process_query(data.query)
    return {"response": response}

if __name__ == "__main__":
    # Run the server on localhost at port 5000
    uvicorn.run("TESTPythonServerBasic:app", host="127.0.0.1", port=5000)
z