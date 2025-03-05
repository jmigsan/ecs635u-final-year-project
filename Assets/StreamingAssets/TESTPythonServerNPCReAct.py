# how to poetry:
# poetry add numba
# poetry add openai-whisper
# poetry add torch torchvision torchaudio --source pytorch-gpu

from fastapi import FastAPI, UploadFile, File
from fastapi.responses import StreamingResponse, FileResponse
import whisper
import tempfile
import os
import torch
from pydantic import BaseModel
from llama_cpp import Llama
import edge_tts
import io

# --- Initialise ---

app = FastAPI()

device = "cuda" if torch.cuda.is_available() else "cpu"
model = whisper.load_model("base", download_root="E:\Code\AppData\whisper").to(device)

# --- Pydantic classes ---

class TranscribeResponse(BaseModel):
    transcription: str

class LlmResponse(BaseModel):
    llm_response: str

class TtsQuery(BaseModel):
    words: str

class NpcQuery(BaseModel):
    query: str

# --- LLM initialise ---

def load_model(model_path):
    llm = Llama(
        model_path=model_path,
        n_ctx=2048,
        n_threads=8,
        n_gpu_layers=-1,
        verbose=False,
    )
    return llm

llm = load_model(r"E:\Code\AppData\LM-Studio\Models\hugging-quants\Llama-3.2-3B-Instruct-Q8_0-GGUF\llama-3.2-3b-instruct-q8_0.gguf")
conversation_history = []

# --- API endpoints ---

@app.post("/llm")
async def llm(npcQuery: NpcQuery) -> LlmResponse:
    # Build message list directly
    system_prompt = "You are a helpful AI assistant that communicates clearly and directly."
    messages = [{"role": "system", "content": system_prompt}] + conversation_history[-5:] + [{"role": "user", "content": npcQuery.query}]

    # Generate response
    response = llm.create_chat_completion(
        messages=messages,
        temperature=0.7,
        max_tokens=256,
        stop=["User:", "Assistant:"],
        stream=False
    )

    response_content = response['choices'][0]['message']['content'].strip()
    print(f"Assistant: {response_content}")

    # Append to history
    conversation_history.append({"role": "user", "content": npcQuery.query})
    conversation_history.append({"role": "assistant", "content": response_content})

    return LlmResponse(llm_response=response_content)

@app.post("/transcribe")
async def transcribe(audio: UploadFile = File(...)) -> TranscribeResponse:
    temp_name = None
    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as temp_audio:
        content = await audio.read()
        temp_audio.write(content)
        temp_audio.flush()
        temp_name = temp_audio.name
    
    result = model.transcribe(temp_name)
    
    os.unlink(temp_name)
    
    return TranscribeResponse(transcription = result["text"])

@app.post("/tts")
async def tts(ttsQuery: TtsQuery):
    # communicate = edge_tts.Communicate(ttsQuery.words, "fil-PH-BlessicaNeural")
    communicate = edge_tts.Communicate(ttsQuery.words, "en-GB-SoniaNeural")
    audio_stream = io.BytesIO()

    async for chunk in communicate.stream():
        if chunk["type"] == "audio":
            audio_stream.write(chunk["data"])

    audio_stream.seek(0)

    return StreamingResponse(audio_stream, media_type="audio/mpeg")

@app.post("/test")
async def test(npcQuery: NpcQuery):
    print(npcQuery.query)
    return npcQuery.query

@app.post("/npcthink")
async def npc_think():
    return

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=8000)