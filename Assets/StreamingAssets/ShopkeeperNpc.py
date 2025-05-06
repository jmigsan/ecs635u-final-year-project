# region LangGraph

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

class Character(BaseModel):
    name: str
    age: int
    gender: str
    occupation: str
    personality: str
    backstory: str

class ConversationMessage(BaseModel):
    message: str
    reasoning: str

class State(TypedDict):
    shopkeeper: Character
    location: str
    time: str
    knowledge: str
    curriculum: str
    conversation: list[ConversationMessage]

structured_llm = llm.with_structured_output(ConversationMessage)

def shopkeeper(state: State):
    prompt = textwrap.dedent(
        f"""
        You are {state['shopkeeper'].name}, a {state['shopkeeper'].occupation} in a peaceful, calming coastal town. You are a NPC in a video game. You are working at your job.
        Your purpose is to welcome and assist visitors in a warm, professional, and in-character manner. Be unique and show your character in how you talk.

        YOUR PROFILE:
        {state['shopkeeper']}

        LOCATION:
        {state['location']}

        TIME:
        {state['time']}

        KNOWLEDGE YOU KNOW:
        {state['knowledge']}

        CONTEXT:
        - You know everything about your workplace's services, hours, rules, and products.  
        - You are familiar with local lore, common requests, and any in-world terminology.

        PLAYER'S LANGUAGE REQUIREMENTS:
        - {state['curriculum']}

        GOALS & BEHAVIOURS:
        1. Greet any player who approaches.
        2. Listen actively, clarifying ambiguous requests.  
        3. Offer relevant information or services.
        4. If you can't fulfill a request, politely redirect or escalate.
        5. Close each interaction warmly.

        CONSTRAINTS:
        - Stay in character—-never break the fourth wall.  
        - Keep answers concise; avoid unnecessary exposition.  
        - Do not reveal internal game mechanics or meta-information.

        CURRENT CONVERSATION:
        {state['conversation']}
        """
    )

    response = structured_llm.invoke(prompt)

    print("shopkeeper response:", response)

    return {"message": response["message"]}

shopkeeper_workflow = StateGraph(State)

shopkeeper_workflow.add_node("shopkeeper", shopkeeper)

shopkeeper_workflow.add_edge(START, "shopkeeper")
shopkeeper_workflow.add_edge("shopkeeper", END)

# endregion

# region FastAPI

from fastapi import FastAPI, WebSocket, WebSocketDisconnect
import asyncio
from functools import partial

app = FastAPI()

class InitialiseShopkeeper(BaseModel):
    type: Literal["initialise_shopkeeper"]
    character: Character
    location: str
    knowledge: str
    curriculum: str

class UserMessage(BaseModel):
    type: Literal["user_message"]
    player: str
    time: str
    message: str

class DoorMessage(BaseModel):
    type: Literal["door_message"]
    time: str
    action: Literal["entered", "exited"]

class ConversationHistoryItem(BaseModel):
    character: str
    message: str

@app.websocket("/ws/shopkeeper-npc")
async def shopkeeper_npc(websocket: WebSocket):
    await websocket.accept()

    shopkeeper: Character = None
    location: str
    knowledge: str
    curriculum: str
    conversation_history: list[ConversationHistoryItem] = []
    
    try:
        while True:
            raw_data = await websocket.receive_json()
            message_type = raw_data.get("type", "")

            if message_type == "initialise_shopkeeper":
                data = InitialiseShopkeeper(**raw_data)
                shopkeeper = data.character
                location = data.location
                knowledge = data.knowledge
                curriculum = data.curriculum
                continue
                
            if message_type == "user_message":
                data = UserMessage(**raw_data)

                conversation_history.append(ConversationHistoryItem(
                    character=data.player,
                    message=data.message
                ))

                shopkeeper_agent = shopkeeper_workflow.compile()

                response = await asyncio.to_thread(
                    partial(shopkeeper_agent.invoke, {
                        "shopkeeper": shopkeeper,
                        "location": location,
                        "time": data.time,
                        "knowledge": knowledge,
                        "curriculum": curriculum,
                        "conversation": conversation_history
                    })
                )

                print(f"Shopkeeper agent response:", response)

                await websocket.send_json({
                    "type": "response",
                    "message": response["message"]
                })

                conversation_history.append(ConversationHistoryItem(
                    character=shopkeeper.name,
                    message=response["message"]
                ))
                continue

            elif message_type == "door_message":
                data = DoorMessage(**raw_data)
                conversation_history.append(ConversationHistoryItem(
                    character="System",
                    message=f"*The player has {data.action} the building through the door. It is now {data.time}.*"
                ))
                continue

            elif message_type == "heartbeat":
                await websocket.send_json({"type": "heartbeat_ack"})
                continue
                
            else:
                print(f"Unknown message type: {message_type}")
                continue
    
    except WebSocketDisconnect:
        print("Client disconnected")

# endregion

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

# region Uvicorn

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("ShopkeeperNpc:app", host="127.0.0.1", port=8001, reload=True)

# endregion