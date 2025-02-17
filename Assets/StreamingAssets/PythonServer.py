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

# --- Helper functions ---

def load_model(model_path):
    llm = Llama(
        model_path=model_path,
        n_ctx=2048,
        n_threads=8,
        n_gpu_layers=-1,
        verbose=False
    )
    return llm

def format_prompt(user_input, history):
    system_prompt = "You are a helpful AI assistant that communicates clearly and directly, provides accurate information while acknowledging uncertainty, thinks through problems step-by-step, engages naturally in conversation while staying focused on the task at hand, maintains appropriate boundaries, and aims to be genuinely useful to users while avoiding potential harm."
    prompt = f"[INST] <<SYS>>\n{system_prompt}\n<</SYS>>\n\n"
    
    for msg in history[-5:]:  
        prompt += f"{msg['role']}: {msg['content']}\n"
    
    prompt += f"User: {user_input}\nAssistant: [/INST]"
    return prompt

# --- LLM initialise ---

llm = load_model(r"E:\Code\AppData\LM-Studio\Models\hugging-quants\Llama-3.2-3B-Instruct-Q8_0-GGUF\llama-3.2-3b-instruct-q8_0.gguf")
conversation_history = []

# --- API endpoints ---

@app.post("/transcribe")
async def transcribe_audio(audio: UploadFile = File(...)) -> TranscribeResponse:
    temp_name = None
    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as temp_audio:
        content = await audio.read()
        temp_audio.write(content)
        temp_audio.flush()
        temp_name = temp_audio.name
    
    result = model.transcribe(temp_name)
    
    os.unlink(temp_name)
    
    return TranscribeResponse(transcription = result["text"])

@app.post("/llm")
async def ask_llm(npcQuery: NpcQuery) -> LlmResponse:
    full_prompt = format_prompt(npcQuery.query, conversation_history)

    response = llm.create_chat_completion(
        messages=[{"role": "user", "content": full_prompt}],
        temperature=0.7,
        max_tokens=256,
        stop=["User:", "Assistant:"],
        stream=False
    )

    response_content = response['choices'][0]['message']['content'].strip()
    print(f"Assistant: {response_content}")
    
    conversation_history.append({"role": "user", "content": npcQuery.query})
    conversation_history.append({"role": "assistant", "content": response_content})

    print(LlmResponse(llm_response = response_content), conversation_history)

    return LlmResponse(llm_response = response_content)

@app.post("/tts")
async def npc_speak(ttsQuery: TtsQuery):
    # communicate = edge_tts.Communicate(ttsQuery.words, "fil-PH-BlessicaNeural")
    communicate = edge_tts.Communicate(ttsQuery.words, "en-GB-SoniaNeural")
    audio_stream = io.BytesIO()

    async for chunk in communicate.stream():
        if chunk["type"] == "audio":
            audio_stream.write(chunk["data"])

    audio_stream.seek(0)

    return StreamingResponse(audio_stream, media_type="audio/mpeg")

@app.post("/test")
async def pppp(npcQuery: NpcQuery):
    print(npcQuery.query)
    return npcQuery.query

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=8000)