from typing_extensions import TypedDict
from typing import Literal, Optional
from pydantic import BaseModel
from langchain_google_genai import ChatGoogleGenerativeAI
from langgraph.graph import StateGraph, START, END
import textwrap
import os
from dotenv import load_dotenv
load_dotenv()

llm = ChatGoogleGenerativeAI(model="gemini-2.0-flash")

class CharacterProfile(BaseModel):
    name: str
    age: int
    occupation: str
    personality: str
    current_life_stage: str
    primary_goal: str
    backstory: str
    how_they_feel_about_current_life: str

class InitialiseCharacter(BaseModel):
    type: Literal["initialise_character"]
    character: CharacterProfile

class ConversationMessage(BaseModel):
    character: str
    message: str
    target: str

class PlayerInterruption(BaseModel):
    type: Literal["player_interruption"]
    message: str

class State(TypedDict):
    character: CharacterProfile
    conversation: list[ConversationMessage]
    character_response: Optional[ConversationMessage]

structured_conversation_llm = llm.with_structured_output(ConversationMessage)

def engage_conversation(state: State):
    print("State:", state)

    prompt = textwrap.dedent(
        f"""You are a character. This is your character profile:
            {state["character"]}

            You are engaging in a conversation.
            Here is the conversation history:
            {state["conversation"]}

            This is the last message spoken to you:
            {state['conversation'][-1]}
        """)
    
    response = structured_conversation_llm.invoke(prompt)
    print("conversation response:", response)
    return {"character_response": response}

conversation_workflow = StateGraph(State)

conversation_workflow.add_node("engage_conversation", engage_conversation)

conversation_workflow.add_edge(START, "engage_conversation")
conversation_workflow.add_edge("engage_conversation", END)

# region FastAPI

from fastapi import FastAPI, WebSocket, WebSocketDisconnect, UploadFile, File
import time
import asyncio
from functools import partial
import os
import edge_tts
import io
from fastapi.responses import StreamingResponse
import requests

app = FastAPI()

@app.websocket("/ws/individual-reserved-brain")
async def individual_reserved_brain(websocket: WebSocket):
    await websocket.accept()

    character_profile: Optional[CharacterProfile] = None
    conversation: list[ConversationMessage] = []

    try:
        while True:
            raw_data = await websocket.receive_json()
            message_type = raw_data.get("type", "")

            if message_type == "initialise_character":
                data = InitialiseCharacter(**raw_data)
                character_profile = data.character

            elif message_type == "player_interruption":
                data = PlayerInterruption(**raw_data)
                target_name = character_profile.name if character_profile is not None else "You"
                conversation.append(ConversationMessage(
                    character="Player",
                    message=data.message,
                    target=target_name))
                
                agent = conversation_workflow.compile()
                response = await asyncio.to_thread(
                    partial(agent.invoke, {
                        "character": character_profile,
                        "conversation" : conversation,
                        "character_response": None
                    })
                )
                print(f"Conversation agent response:", response)

                character_response = response["character_response"]
                conversation.append(character_response)

                await websocket.send_json({
                    "type": "character_response",
                    "message": character_response.message,
                    "target": character_response.target
                })
                continue
            
            elif message_type == "heartbeat":
                await websocket.send_json({"type": "heartbeat_ack"})
                continue

            else:
                print(f"Unknown message type: {message_type}")
                continue
                

    except WebSocketDisconnect:
        print("Client disconnected")

# region TTS

import edge_tts
import io
from fastapi.responses import StreamingResponse

class TtsQuery(BaseModel):
    words: str
    voice: str

@app.post("/tts")
async def npc_speak(ttsQuery: TtsQuery):
    # communicate = edge_tts.Communicate(ttsQuery.words, "fil-PH-BlessicaNeural")
    # voices: "en-GB-LibbyNeural", "en-AU-NatashaNeural", "en-GB-ThomasNeural"
    print("TTS:", ttsQuery.words, ttsQuery.voice)

    communicate = edge_tts.Communicate(ttsQuery.words, ttsQuery.voice)
    audio_stream = io.BytesIO()

    async for chunk in communicate.stream():
        if chunk["type"] == "audio" and "data" in chunk:
            audio_stream.write(chunk["data"])

    audio_stream.seek(0)

    return StreamingResponse(audio_stream, media_type="audio/mpeg")

# endregion

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("IndividualReservedBrain:app", host="127.0.0.1", port=8002, reload=True)

