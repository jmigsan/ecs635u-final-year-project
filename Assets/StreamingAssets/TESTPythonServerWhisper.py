# how to poetry:
# poetry add numba
# poetry add openai-whisper
# poetry add torch torchvision torchaudio --source pytorch-gpu

from fastapi import FastAPI, UploadFile, File
import whisper
import tempfile
import os
import torch

app = FastAPI()

device = "cuda" if torch.cuda.is_available() else "cpu"
model = whisper.load_model("base", download_root="E:\Code\AppData\whisper").to(device)

@app.post("/transcribe")
async def transcribe_audio(audio: UploadFile = File(...)):
    temp_name = None
    # Create temp file that deletes itself but closes before Whisper reads it
    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as temp_audio:
        content = await audio.read()
        temp_audio.write(content)
        temp_audio.flush()
        temp_name = temp_audio.name
    
    # Now the file is closed but exists
    result = model.transcribe(temp_name)
    
    # Delete it after Whisper is done
    os.unlink(temp_name)
    
    return {"transcription": result["text"]}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=8000)