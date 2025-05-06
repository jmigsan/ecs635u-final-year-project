# req characters enter location
# director gets list of characters and their personalities, goals, etc.
# director gets location, time of day, story, potential plot points? yeah sure. hard code it for now. :/
# make 50 long dialogue.
# control characters like puppets. tells them what to say.
# if player interrupts, note where in dialogue characters are, rewrite next lines in story of what characters do. write 20 next lines
# if a character asks the player a question in the lines generated, stop generating the rest of the lines.
# when a character asks the player a question, if the player is silent for 5 secs, timeout and take the player's silence as the respone. then generate remainder 20 next lines.
# when conversation is finished, tell characters to go do their own thing, give up control of characters.

# region LangGraph

import textwrap
from typing_extensions import TypedDict
from typing import Literal, Optional, cast
from langchain_google_genai import ChatGoogleGenerativeAI
from langgraph.graph import StateGraph, START, END
import os
from dotenv import load_dotenv
from pydantic import BaseModel, Field

load_dotenv()

llm = ChatGoogleGenerativeAI(model="gemini-2.0-flash")

class Character(BaseModel):
    name: str
    age: int
    gender: str
    occupation: str
    personality: str
    backstory: str

class Direction(BaseModel):
    character: str
    words: str
    target: str
    reasoning: str

class Directions(BaseModel):
    directions: list[Direction]

class NpcConversationSummaries(BaseModel):
    character: str
    summary: str

class Summaries(BaseModel):
    summaries: list[NpcConversationSummaries]

class PreviousNpcConversations(BaseModel):
    character: str
    player_conversations: list
    group_conversations: list

class State(TypedDict):
    characters: list[Character]
    location: str
    time: str
    purpose: str
    player: str
    directions: Optional[list[Direction]]
    previous_npc_conversations: Optional[list[PreviousNpcConversations]]
    summaries: Optional[list[NpcConversationSummaries]]
    curriculum: str

structured_summariser_llm = llm.with_structured_output(Summaries)

def summariser(state: State):
    prompt = textwrap.dedent(
        f"""
        Summarise the conversations of each of these characters based on the provided history:
        {state['characters']}

        Here is their conversation histories:
        {state.get('previous_npc_conversations', 'No previous history provided.')}
        """)

    print("Summariser prompt:", prompt)
    response = structured_summariser_llm.invoke(prompt)

    print("summariser response:", response)
    return {"summaries": response}

structured_director_llm = llm.with_structured_output(Directions)

def director(state: State):
    prompt = textwrap.dedent(
        f"""
        Create an authentic conversation between characters in a peaceful, calming coastal town.
        This conversation is for a video game with believable non-player characters.

        CHARACTERS:
        {state['characters']}

        SUMMARIES OF PREVIOUS CONVERSATIONS OF EACH CHARACTER:
        {state['summaries']}

        LOCATION:
        {state['location']}

        TIME:
        {state['time']}

        PURPOSE:
        {state['purpose']}

        CONVERSATION STRUCTURE:
        - Start with natural greetings and acknowledgment of each other
        - Progress to casual topics relevant to their location and daily lives
        - Gradually weave in key information related to the main purpose
        - Create a natural conclusion

        RELATIONSHIP DYNAMICS:
        - Establish clear pre-existing relationships between characters through subtle references
        - Show their history through inside jokes, shared memories, or familiar patterns
        - Give each character a unique conversational style that reflects their personality

        CONVERSATION TOPICS:
        - Daily routines and current activities
        - Local town happenings and community events
        - Seasonal changes or weather observations
        - Personal stories that reveal character backgrounds
        - If the purpose involves specific information, weave it naturally into these topics

        DIALOGUE GUIDELINES:
        - Use natural speech with pauses, interruptions, and conversational markers
        - Include regional expressions or local references that fit the coastal setting
        - Vary sentence length and complexity based on each character's personality
        - Create moments of humor, nostalgia, or mild disagreement to add depth
        - Maintain a peaceful, slice-of-life atmosphere throughout
        - DO NOT target the player character.

        PLAYER'S LANGUAGE REQUIREMENTS:
        - {state['curriculum']}

        OUTPUT FORMAT REQUIREMENTS:
        - Each dialogue line must belong to a specific character
        - Each line must include reasoning that explains the character's motivation or thought process
        - Lines should only contain spoken dialogue (no action descriptions in the dialogue)
        - Target each line to the appropriate character(s) being addressed
        - DO NOT target multiple characters. If both characters are being addressed, only address one. Assume the other characters will hear it too.

        EXAMPLE EXCHANGE:
        [Character A - Sam]: Good morning, Eliza! Those fresh peaches look wonderful today.
        [Reasoning]: A notices the seasonal fruit and wants to start a pleasant conversation with the shopkeeper
        [Target]: Eliza

        [Character B - Eliza]: Morning, Sam! Just got them in from the Henderson farm. How's that boat repair coming along?
        [Reasoning]: Eliza acknowledges the compliment but shifts to personal matters, showing their established relationship
        [Target]: Sam
        """
    )

    raw_response = structured_director_llm.invoke(prompt)
    response_obj = cast(Directions, raw_response)
    actual_directions = response_obj.directions

    completion_message = Direction(
        character="System",
        words="Conversation Complete",
        target="SceneDirector",
        reasoning="Conversation is complete."
    )
    
    actual_directions.append(completion_message)  # Append to the list

    print("director response:", actual_directions)

    return {"directions": actual_directions}

begin_direction_workflow = StateGraph(State)

begin_direction_workflow.add_node("summariser", summariser)
begin_direction_workflow.add_node("director", director)

begin_direction_workflow.add_edge(START, "summariser")
begin_direction_workflow.add_edge("summariser", "director")
begin_direction_workflow.add_edge("director", END)

def player_interruption_director(state: State):
    prompt = textwrap.dedent(
        f"""
        Continue an authentic conversation that has been interrupted by the player character in a peaceful, calming coastal town.

        NON-PLAYER CHARACTERS (NPCs):
        {state['characters']}

        SUMMARIES OF PREVIOUS CONVERSATIONS OF EACH NON-PLAYER CHARACTER:
        {state['summaries']}

        PLAYER CHARACTER:
        {state['player']}

        LOCATION:
        {state['location']}

        TIME:
        {state['time']}

        PURPOSE:
        {state['purpose']}

        PREVIOUS CONVERSATION (including player's interruption):
        {state['directions']}

        CONVERSATION CONTINUATION GUIDELINES:
        - Acknowledge the player's input immediately with authentic reactions from NPCs
        - Naturally integrate the player into the existing conversation dynamics
        - Maintain previously established character personalities and relationship patterns
        - Remember and reference information shared earlier in the conversation
        - If the player's input is off-topic or strange, show realistic but polite responses from NPCs
        - Use the player's words/actions as a springboard for developing the conversation in new directions

        NPC RESPONSES TO PLAYER:
        - Initial reactions should directly address what the player said/did
        - NPCs should show different reaction styles based on their personality
        - Some NPCs might engage deeply with the player while others might respond more briefly
        - NPCs might ask follow-up questions to draw the player deeper into conversation
        - If player shares personal information, NPCs should remember and reference it later

        MAINTAINING AUTHENTICITY:
        - NEVER write dialogue for {state['player']} - only NPCs should have dialogue lines
        - Keep all NPCs at the location; they don't leave during this interaction
        - If the player says something confusing or irrelevant, NPCs should show realistic confusion
        - If the player's input relates to the purpose of the conversation, use it to further that purpose
        - NPCs should continue to interact with each other, not just with the player

        OUTPUT FORMAT REQUIREMENTS:
        - Each NPC dialogue line must include reasoning that explains their thought process
        - Lines should only contain spoken dialogue (no narration or action descriptions)
        - Target each line to either another NPC or the player character
        - Include approximately 5-7 exchanges per NPC to create a substantial continuation
        - End the conversation naturally if it reaches a logical conclusion

        PLAYER'S LANGUAGE REQUIREMENTS:
        - {state['curriculum']}

        EXAMPLE OF HANDLING PLAYER INPUT:
        [Previous NPC line]: I heard the fishing has been really good at the south pier lately.
        [Player Input]: I caught a massive tuna there yesterday!
        
        [NPC Response]: No kidding? A tuna this close to shore? That's unusual for this time of year! Did you use the special lures old Barney sells?
        [Reasoning]: Thomas is surprised by this information and wants to learn more, showing his knowledge of local fishing
        [Target]: {state['player']}
        
        [Another NPC]: Thomas here would know - he's been mapping the fish migrations for the past decade. The water temperature must be changing again.
        [Reasoning]: Maria provides context about Thomas to the player while adding her own observation about environmental changes
        [Target]: {state['player']}
        """
    )
    
    raw_response = structured_director_llm.invoke(prompt)
    response_obj = cast(Directions, raw_response)
    actual_directions = response_obj.directions
    
    completion_message = Direction(
        character="System",
        words="Conversation Complete",
        target="SceneDirector",
        reasoning="Conversation is complete."
    )
    
    actual_directions.append(completion_message)

    print("director response:", actual_directions)

    return {"directions": actual_directions}

player_interruption_workflow = StateGraph(State)

player_interruption_workflow.add_node("summariser", summariser)
player_interruption_workflow.add_node("player_interruption_director", player_interruption_director)

player_interruption_workflow.add_edge(START, "summariser")
player_interruption_workflow.add_edge("summariser", "player_interruption_director")
player_interruption_workflow.add_edge("player_interruption_director", END)

class SummariseConversationState(TypedDict):
    conversation_history: list[Direction]
    summary: Optional[str]

def summarise_conversation(state: SummariseConversationState):
    prompt = textwrap.dedent(
        f"""
        Make a brief summary of this conversation:
        {state['conversation_history']}
        """
    )
    
    response = llm.invoke(prompt)

    print("summarise response:", response)

    return {"summary": response.content}

summarise_conversation_workflow = StateGraph(SummariseConversationState)

summarise_conversation_workflow.add_node("summarise_conversation", summarise_conversation)

summarise_conversation_workflow.add_edge(START, "summarise_conversation")
summarise_conversation_workflow.add_edge("summarise_conversation", END)

# endregion

# region FastAPI

from fastapi import FastAPI, WebSocket, WebSocketDisconnect
import asyncio
from functools import partial
import json

app = FastAPI()

class BeginSceneDirection(BaseModel):
    type: Literal["begin_scene_direction"]
    characters: list[Character]
    location: str
    time: str
    purpose: str
    player: str
    previously: Optional[str]
    curriculum: str

class PlayerInterruption(BaseModel):
    type: Literal["player_interruption"]
    action: str
    target: str

class CompletedDirection(BaseModel):
    type: Literal["completed_direction"]
    character: str
    words: str
    target: str

class DirectionHistoryItem(BaseModel):
    character: str
    words: str
    target: str

@app.websocket("/ws/scene-director")
async def scene_director(websocket: WebSocket):
    await websocket.accept()

    direction_history: list[DirectionHistoryItem] = []

    characters: list[Character] = [] 
    location: str = ""
    time: str = ""
    purpose: str = ""
    player: str = ""
    previously: Optional[str] = None
    curriculum: str = ""

    try:
        while True:
            raw_data = await websocket.receive_json()
            message_type = raw_data.get("type", "")

            if message_type == "begin_scene_direction":
                print(raw_data)

                data = BeginSceneDirection(**raw_data)

                begin_scene_director_agent = begin_direction_workflow.compile()

                characters = data.characters
                location = data.location
                time = data.time
                purpose = data.purpose
                player = data.player
                previously = data.previously
                curriculum = data.curriculum

                response = await asyncio.to_thread(
                    partial(begin_scene_director_agent.invoke, {
                        "characters": characters,
                        "location": location,
                        "time": time,
                        "purpose": purpose,
                        "directions": None,
                        "player": player,
                        "previous_npc_conversations": previously,
                        "summaries": None,
                        "curriculum": curriculum
                    })
                )

                directions = response["directions"]

                print("Begin director response", directions)

                direction_dicts = [direction.model_dump() for direction in directions]

                print("begin direction dicts", direction_dicts)
                
                await websocket.send_json({
                    "type": "directions",
                    "directions": direction_dicts
                })
                continue

            elif message_type == "completed_direction":
                data = CompletedDirection(**raw_data)
                
                completed_direction = DirectionHistoryItem(
                    character=data.character,
                    words=data.words,
                    target=data.target
                )

                direction_history.append(completed_direction)
                print("Direction history:", direction_history)
                continue

            elif message_type == "player_interruption":
                data = PlayerInterruption(**raw_data)

                player_interruption_direction = DirectionHistoryItem(
                    character=player,
                    words=data.action,
                    target=data.target
                )

                direction_history.append(player_interruption_direction)
                print("Direction history (player interruption):", direction_history)

                player_interruption_director_agent = player_interruption_workflow.compile()

                response = await asyncio.to_thread(
                    partial(player_interruption_director_agent.invoke, {
                        "characters": characters,
                        "location": location,
                        "time": time,
                        "purpose": purpose,
                        "directions": direction_history,
                        "player": player,
                        "previous_npc_conversations": previously,
                        "summaries": None,
                        "curriculum": curriculum
                    })
                )

                directions = response["directions"]

                print("Player interruption response", directions)

                direction_dicts = [direction.model_dump() for direction in directions]
                
                await websocket.send_json({
                    "type": "directions",
                    "directions": direction_dicts
                })
                continue

            elif message_type == "get_direction_history_summary":
                summarise_conversation_agent = summarise_conversation_workflow.compile()

                response = await asyncio.to_thread(
                    partial(summarise_conversation_agent.invoke, {
                        "conversation_history": direction_history,
                        "summary": None
                    })
                )

                await websocket.send_json({
                    "type": "direction_history_summary",
                    "summary": response["summary"]
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

# region Whisper

import requests
import io
from fastapi import File, UploadFile
from pydantic import BaseModel

class TranscribeResponse(BaseModel):
    transcription: str

@app.post("/transcribe")
async def transcribe_audio(audio: UploadFile = File(...)) -> TranscribeResponse:
    url = "https://api.lemonfox.ai/v1/audio/transcriptions"

    api_key = os.getenv("LEMONFOX_API_KEY")
    headers = {
        "Authorization": f"Bearer {api_key}"
    }

    data = {
        "language": "japanese",
        "response_format": "json"
    }
    
    content = await audio.read()
    file_obj = io.BytesIO(content)
    file_obj.name = audio.filename or "audio.wav"
    files = {
        "file": file_obj
    }
    
    response = requests.post(url, headers=headers, files=files, data=data)
    
    if response.status_code == 200:
        result = response.json()
        transcription = result.get("text", "")
        print("Transcription:", transcription, "\n")
        return TranscribeResponse(transcription=transcription)
    else:
        print(f"Error: {response.status_code}, {response.text}")
        return TranscribeResponse(transcription="Error during transcription")

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
    uvicorn.run("SceneDirector:app", host="127.0.0.1", port=8000, reload=True)

# endregion