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

class ConversationHistoryItem(BaseModel):
    character: str
    message: str

class ScriptedConversationHistoryItem(BaseModel):
    time: str
    summary: str

class State(TypedDict):
    character: Character
    location: str
    time: str
    knowledge: str
    curriculum: str
    previous_conversations: list[ConversationHistoryItem]
    previous_group_conversations: list[ScriptedConversationHistoryItem]

structured_llm = llm.with_structured_output(ConversationMessage)

def directable(state: State):
    prompt = textwrap.dedent(
        f"""
        You are {state['character'].name}, a {state['character'].occupation} in a peaceful, calming coastal town. You are a NPC in a video game.
        You're here to chat with players while staying in character.

        YOUR PROFILE:
        {state['character']}

        LOCATION:
        {state['location']}

        TIME:
        {state['time']}

        KNOWLEDGE YOU KNOW:
        {state['knowledge']}

        SUMMARIES OF PREVIOUS GROUP CONVERSATIONS:
        {state['previous_group_conversations']}

        CONTEXT:
        - You know everything about your workplace's services, hours, rules, and products.  
        - You are familiar with local lore, common requests, and any in-world terminology.

        PLAYER'S LANGUAGE REQUIREMENTS:
        - {state['curriculum']}

        STYLE:
        - Use conversational turns that feel natural: ask follow-up questions, share anecdotes.
        - Don't lecture—keep descriptions short, use contractions for a casual vibe.

        GOALS & BEHAVIOURS:
        1. Greet any player who approaches.
        2. Listen actively, clarifying ambiguous requests.  
        3. Offer relevant information or services.
        4. If you can't fulfill a request, politely redirect or escalate.
        5. Close each interaction warmly.

        CONSTRAINTS:
        - Never break character or mention game mechanics.
        - Keep responses under 2-3 sentences unless a story nugget demands more.  
        - Vary your dialogue so repeat visits feel fresh.

        CURRENT CONVERSATION:
        {state['previous_conversations']}

        Provide your response in two parts:
        1. MESSAGE: The exact words you will say.
        2. REASONING: A very brief explanation (1-2 sentences) of your thought process.
        """
    )

    response = structured_llm.invoke(prompt)

    print("directable response:", response)

    return {"message": response["message"]}

directable_workflow = StateGraph(State)

directable_workflow.add_node("directable", directable)

directable_workflow.add_edge(START, "directable")
directable_workflow.add_edge("directable", END)

# endregion

# region FastAPI

from fastapi import FastAPI, WebSocket, WebSocketDisconnect
import asyncio
from functools import partial

app = FastAPI()

class InitialiseDirectable(BaseModel):
    type: Literal["initialise_directable"]
    character: Character
    location: str
    knowledge: str
    curriculum: str

class UserMessage(BaseModel):
    type: Literal["user_message"]
    player: str
    time: str
    message: str

class ScriptedConversationMessage(BaseModel):
    type: Literal["scripted_conversation"]
    time: str
    summary: str

@app.websocket("/ws/directable-npc")
async def directable_npc(websocket: WebSocket):
    await websocket.accept()

    character: Character = None
    location: str
    knowledge: str
    curriculum: str

    conversation_history: list[ConversationHistoryItem] = []
    scripted_conversation_history: list[ScriptedConversationHistoryItem] = []
    
    try:
        while True:
            raw_data = await websocket.receive_json()
            message_type = raw_data.get("type", "")

            if message_type == "initialise_directable":
                data = InitialiseDirectable(**raw_data)
                character = data.character
                location = data.location
                knowledge = data.knowledge
                curriculum = data.curriculum
                continue
                
            elif message_type == "user_message":
                data = UserMessage(**raw_data)

                conversation_history.append(ConversationHistoryItem(
                    character=data.player,
                    message=data.message
                ))

                directable_agent = directable_workflow.compile()

                response = await asyncio.to_thread(
                    partial(directable_agent.invoke, {
                        "character": character,
                        "location": location,
                        "time": data.time,
                        "knowledge": knowledge,
                        "curriculum": curriculum,
                        "previous_conversations": conversation_history,
                        "previous_group_conversations": scripted_conversation_history
                    })
                )

                print(f"Directable agent response:", response)

                await websocket.send_json({
                    "type": "response",
                    "message": response["message"]
                })

                conversation_history.append(ConversationHistoryItem(
                    character=character.name,
                    message=response["message"]
                ))
                continue

            elif message_type == "scripted_conversation":
                data = ScriptedConversationMessage(**raw_data)
                scripted_conversation_history.append(ScriptedConversationHistoryItem(
                    time=data.time,
                    summary=data.summary
                ))
                print("Scripted conversation history:", scripted_conversation_history)
                continue

            elif message_type == "get_all_conversation_history":
                await websocket.send_json({
                    "type": "all_conversation_history",
                    "conversation_history": [item.model_dump() for item in conversation_history],
                    "scripted_conversation_history": [item.model_dump() for item in scripted_conversation_history]
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
    uvicorn.run("DirectableNpc:app", host="127.0.0.1", port=8002, reload=True)

# endregion