# how to poetry:
# poetry add numba
# poetry add openai-whisper
# poetry add torch torchvision torchaudio --source pytorch-gpu

from fastapi import FastAPI, UploadFile, File
import whisper
import tempfile
import os
import torch
from pydantic import BaseModel
from llama_cpp import Llama

# --- Initialise ---

app = FastAPI()

device = "cuda" if torch.cuda.is_available() else "cpu"
model = whisper.load_model("base", download_root="E:\Code\AppData\whisper").to(device)

llm = load_model(r"E:\Code\AppData\LM-Studio\Models\hugging-quants\Llama-3.2-3B-Instruct-Q8_0-GGUF\llama-3.2-3b-instruct-q8_0.gguf")
conversation_history = []

# --- Pydantic classes ---

class TranscribeResponse(BaseModel):
    transcription: str

class LlmResponse(BaseModel):
    llm_response: str

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
    system_prompt = "You are a helpful AI assistant. Respond conversationally."
    prompt = f"[INST] <<SYS>>\n{system_prompt}\n<</SYS>>\n\n"
    
    for msg in history[-5:]:  
        prompt += f"{msg['role']}: {msg['content']}\n"
    
    prompt += f"User: {user_input}\nAssistant: [/INST]"
    return prompt

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
    
    return TranscribeResponse({"transcription": result["text"]})

@app.post("/llm")
async def ask_llm(query: str) -> LlmResponse:
    full_prompt = format_prompt(query, conversation_history)

    response = llm.create_chat_completion(
        messages=[{"role": "user", "content": full_prompt}],
        temperature=0.7,
        max_tokens=256,
        stop=["User:", "Assistant:"],
        stream=False
    )

    response_content = response['choices'][0]['message']['content'].strip()
    print(f"Assistant: {response_content}")
    
    conversation_history.append({"role": "user", "content": query})
    conversation_history.append({"role": "assistant", "content": response_content})

    return LlmResponse({"llm_response": response_content})

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=8000)