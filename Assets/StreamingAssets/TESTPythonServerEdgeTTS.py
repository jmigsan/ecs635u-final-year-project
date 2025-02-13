from fastapi import FastAPI, UploadFile, File, BackgroundTasks
from fastapi.responses import FileResponse
import whisper
import tempfile
import os
import torch
import edge_tts

app = FastAPI()
device = "cuda" if torch.cuda.is_available() else "cpu"

# Load the Whisper model (adjust the download_root as needed)
model = whisper.load_model("base", download_root=r"E:\Code\AppData\whisper").to(device)

@app.post("/transcribe")
async def transcribe_audio(
    audio: UploadFile = File(...), background_tasks: BackgroundTasks = None
):
    # Save the uploaded audio temporarily for Whisper to process
    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as temp_audio:
        content = await audio.read()
        temp_audio.write(content)
        temp_audio.flush()
        temp_audio_name = temp_audio.name

    # Transcribe the audio using Whisper
    result = model.transcribe(temp_audio_name)
    transcription_text = result["text"]
    os.unlink(temp_audio_name)  # Clean up the temporary audio file

    # Synthesize speech from the transcription using Edge TTS.
    # Create a temporary file to hold the synthesized MP3.
    with tempfile.NamedTemporaryFile(suffix=".mp3", delete=False) as temp_tts_file:
        tts_filename = temp_tts_file.name

    # Choose a voice (for example, 'en-US-JennyNeural')—feel free to adjust as needed.
    communicator = edge_tts.Communicate(transcription_text, voice="fil-PH-BlessicaNeural")
    await communicator.save(tts_filename)

    # Schedule the deletion of the temporary TTS file after sending the response.
    if background_tasks is not None:
        background_tasks.add_task(os.unlink, tts_filename)

    # Return the synthesized audio file so Unity can play it.
    return FileResponse(tts_filename, media_type="audio/mpeg", filename="transcription.mp3")

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=8000)
