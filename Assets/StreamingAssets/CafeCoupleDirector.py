# get character descriptions from unity
# feed it into prompt
# do langgraph to make a conversation between the both of them
# tell unity to give back their conversation list
# when conversation done, do a wait timer, then ask it to make a new conversation, given the previous conversation.

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

class CharacterProfile(BaseModel):
    name: str
    age: int
    occupation: str
    personality: str
    current_life_stage: str
    primary_goal: str
    backstory: str
    how_they_feel_about_current_life: str

class ConversationMessage(BaseModel):
    character: str
    message: str
    target: str

class BeginState(TypedDict):
    character_1: CharacterProfile
    character_2: CharacterProfile
    relationship: str
    tone: str
    conversation: Optional[list[ConversationMessage]]

class ContinueState(TypedDict):
    character_1: CharacterProfile
    character_2: CharacterProfile
    relationship: str
    previous_tone: str
    previous_conversation: list[ConversationMessage]
    new_tone: Optional[str]
    new_conversation: Optional[list[ConversationMessage]]
    
structured_conversation_llm = llm.with_structured_output(list[ConversationMessage])

def begin_conversation(state: BeginState):
    print("State:", state)

    prompt = textwrap.dedent(
        f"""
        Write a lengthy dialogue between two characters.

        Character 1:
        {state["character_1"].dict()}

        Character 2:
        {state["character_2"].dict()}

        This is their relationship with each other:
        {state["relationship"]}

        This is their tone:
        {state["tone"]}

        They are sitting next to each other at the same table in a coffee shop.
        The entire scene takes place within the coffee shop setting, without either character relying on external props or actions beyond talking to each other.
        Make it lengthy - at least 30 turns per character.
        """
    )

    response = structured_conversation_llm.invoke(prompt)
    print("conversation response:", response)

    return {"conversation": response}

def continue_conversation_new_tone(state: ContinueState):
    print("State:", state)

    prompt = textwrap.dedent(
        f"""
        These two characters have just had this conversation. They have finished.

        Character 1:
        {state["character_1"].dict()}

        Character 2:
        {state["character_2"].dict()}

        Conversation:
        {state["conversation"]}

        This is their relationship with each other:
        {state["relationship"]}

        This is the tone of their previous conversation:
        {state["tone"]}

        Continue a conversation between them. They have sat in silence. And after a while start another conversation.
        In one word, generate a new tone for a new conversation between them.
        """
    )
    response = llm.invoke(prompt)
    print("new_tone response:", response)

    return {"new_tone": response}

def continue_conversation(state: ContinueState):
    print("State:", state)

    prompt = textwrap.dedent(
        f"""
        These two characters have just had this conversation. They have finished.

        Character 1:
        {state["character_1"].dict()}

        Character 2:
        {state["character_2"].dict()}

        Conversation:
        {state["conversation"]}

        This is their relationship with each other:
        {state["relationship"]}

        This is the tone of their previous conversation:
        {state["tone"]}

        They are sitting next to each other at the same table in a coffee shop.

        Continue a conversation between them. They have sat in silence. And after a while start another conversation.
        
        This is the new tone for their conversation:
        {state["new_tone"]}

        The entire scene takes place within the coffee shop setting, without either character relying on external props or actions beyond talking to each other.
        Make it lengthy - at least 30 turns per character.
        """
    )

    response = structured_conversation_llm.invoke(prompt)
    print("new_conversation response:", response)

    return {
        "new_tone": state["new_tone"], 
        "new_conversation": response
        }

begin_conversation_workflow = StateGraph(BeginState)

begin_conversation_workflow.add_node("begin_conversation", begin_conversation)

begin_conversation_workflow.add_edge(START, "begin_conversation")
begin_conversation_workflow.add_edge("begin_conversation", END)

continue_conversation_workflow = StateGraph(ContinueState)

continue_conversation_workflow.add_node("continue_conversation_new_tone", continue_conversation_new_tone)
continue_conversation_workflow.add_node("continue_conversation", continue_conversation)

continue_conversation_workflow.add_edge(START, "continue_conversation_new_tone")
continue_conversation_workflow.add_edge("continue_conversation_new_tone", "continue_conversation")
continue_conversation_workflow.add_edge("continue_conversation", END)

# endregion

# region FastAPI

from fastapi import FastAPI, WebSocket, WebSocketDisconnect
import asyncio
from functools import partial

app = FastAPI()

class BeginCoupleConversation(BaseModel):
    type: Literal["begin_couple_conversation"]
    character_1: CharacterProfile
    character_2: CharacterProfile
    relationship: str
    tone: str

class ContinueCoupleConversation(BaseModel):
    type: Literal["continue_couple_conversation"]
    character_1: CharacterProfile
    character_2: CharacterProfile
    relationship: str
    tone: str
    conversation: list[ConversationMessage]

@app.websocket("/ws/cafe-couple-director")
async def cafe_couple_director(websocket: WebSocket):
    await websocket.accept()

    character_1: CharacterProfile
    character_2: CharacterProfile
    conversation: list[ConversationMessage]
    relationship: str
    tone: str
    
    try:
        while True:
            raw_data = await websocket.receive_json()
            message_type = raw_data.get("type", "")

            if message_type == "begin_couple_conversation":
                data = BeginCoupleConversation(**raw_data)
                character_1 = data.character_1
                character_2 = data.character_2
                relationship = data.relationship
                tone = data.tone

                print(f"Character 1:", character_1)
                print(f"Character 2:", character_2)

                begin_conversation_agent = begin_conversation_workflow.compile()

                response = await asyncio.to_thread(
                    partial(begin_conversation_agent.invoke, {
                        "character_1": character_1,
                        "character_2": character_2,
                        "relationship": relationship,
                        "tone": tone,
                        "conversation": None
                    })
                )

                conversation = response["conversation"]

                print(f"Make conversation agent response:", response)

                await websocket.send_json({
                    "type": "conversation",
                    "conversation": conversation
                })
                continue
                
            if message_type == "continue_couple_conversation":
                data = ContinueCoupleConversation(**raw_data)
                character_1 = data.character_1
                character_2 = data.character_2
                relationship = data.relationship
                tone = data.tone

                continue_conversation_agent = continue_conversation_workflow.compile()

                response = await asyncio.to_thread(
                    partial(continue_conversation_agent.invoke, {
                        "character_1" : character_1,
                        "character_2" : character_2,
                        "relationship" : relationship,
                        "previous_tone" : tone,
                        "previous_conversation" : conversation,
                        "new_tone" : None,
                        "new_conversation" : None
                    })
                )

                tone = response["new_tone"]
                conversation = response["new_conversation"]

                print(f"Continue conversation agent response:", response)

                await websocket.send_json({
                    "type": "conversation",
                    "conversation": conversation
                })
                continue

            elif message_type == "heartbeat":
                await websocket.send_json({"type": "heartbeat_ack"})
                
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

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("CafeCoupleDirector:app", host="127.0.0.1", port=8001, reload=True)

