# writer makes a scene
# director follows scene

# region LangGraph

import textwrap
from typing_extensions import TypedDict
from typing import Literal, Optional
from pydantic import BaseModel
from langchain_google_genai import ChatGoogleGenerativeAI
from langgraph.graph import StateGraph, START, END
import os

os.environ['GOOGLE_API_KEY'] = 'AIzaSyBbgaNd1O9xLJK1uai_i8fwGwgMDMRwPjA'
llm = ChatGoogleGenerativeAI(model="gemini-2.0-flash")

from pydantic import BaseModel, Field

# ----- Director BaseModels

class DirectedAction(BaseModel):
    character: str = Field(description="Name of the character that will do the action")
    action: str = Field(description="The action to perform")
    target: str = Field(description="The target of the action (character name or object)")
    message: Optional[str] = Field(description="Message content for talking but this optional and must only be included if the action is 'talk'")

class WhoNext(BaseModel):
    character: Literal["Harry", "Emily", "Violet", "Akira", "Ren", "Julia"]

# ------ Director/Writer BaseModel
class PreviousAction(BaseModel):
    time: str
    character: str
    action: str
    target: str
    message: Optional[str] = None

# ----- Writer BaseModels

class SceneAct(BaseModel):
    content: str = Field(description="The actual content of this section of the scene")
    purpose: str = Field(description="Narrative function this section serves")

class FiveActScene(BaseModel):
    title: str = Field(description="A descriptive title for the scene")
    setting: str = Field(description="The setting of the scene")
    
    exposition: SceneAct = Field(description="Establishes setting, characters present, and immediate context")
    rising_action: SceneAct = Field(description="Creates tension or conflict specific to this scene")
    climax: SceneAct = Field(description="The turning point or moment of highest tension in the scene")
    falling_action: SceneAct = Field(description="Shows immediate consequences of the climax")
    resolution: SceneAct = Field(description="Concludes the scene while connecting to the larger story")
    
class Character(BaseModel):
    name: str
    role: str
    personality: str

# ----- State BaseModels

class PerceptionItem(BaseModel):
    type: str
    entity: str
    description: str

class PossibleAction(BaseModel):
    target: str
    actions: list[str]

class CharacterPerception(BaseModel):
    character: str
    things_character_sees: list[PerceptionItem]
    actions_character_can_do: list[PossibleAction]

class State(TypedDict):
    story: FiveActScene
    what_point_story_is_in: str
    who_should_act_next: WhoNext
    direction: DirectedAction
    previous_actions: list[PreviousAction]
    characters: list[Character]
    character_perceptions: list[CharacterPerception]
    setting: str

# ----- Graph Nodes

# test to see if it's better without the strcutre
structured_writer_llm = llm.with_structured_output(FiveActScene)

def writer_makes_story(state: State):
    story = structured_writer_llm.invoke(textwrap.dedent(
        f"""
        Write an outline for a scene for a romantic slice of life anime.
        However, the story is for a video game. One character is the user, who is the 'Player'.

        Here are your characters.
        {state["characters"]}

        Use this setting:
        {state["setting"]}
        """))

    return {"story": story}

structured_director_llm = llm.with_structured_output(DirectedAction)

def director_1(state: State):

    response = llm.invoke(textwrap.dedent(
        f"""
        Look at this story:
        {state["story"]}

        The story is set in the following setting:
        {state["setting"]}
        
        Here are your characters:
        {state["characters"]}
        
        This is what has happened:
        {state["previous_actions"]}

        Based on the previous actions, determine at what point the story is in.
        """
    ))

    print("director 1 said:", response, "\n")
    return {"what_point_story_is_in": response.content}

structured_director_2_llm = llm.with_structured_output(WhoNext)

def director_2(state: State):

    response = structured_director_2_llm.invoke(textwrap.dedent(
        f"""
        You are a director directing non-player characters to follow a story.

        Look at this story:
        {state["story"]}
        
        Here are your characters:
        {state["characters"]}
        
        This is what has happened:
        {state["previous_actions"]}
        
        You are currently at this point in the story:
        {state["what_point_story_is_in"]}

        However, the story is in a video game. The character 'Player' is not a character you can direct, but instead a character controlled by a user.
        Based on what point in the story you are in and what previous actions have happened, who should act next?
        """
    ))

    print(response)

    return {"who_should_act_next": response}

structured_director_3_llm = llm.with_structured_output(DirectedAction)

def director_3(state: State):
    next_character_name = state["who_should_act_next"].character    
    
    next_character_perception = next(
        (perception for perception in state["character_perceptions"] 
         if perception.character == next_character_name), 
        None
    )

    things_this_character_sees = []
    actions_this_character_can_do = []
    
    if next_character_perception is not None:
        print("Things character sees:", next_character_perception.things_character_sees, "\n")
        things_this_character_sees = next_character_perception.things_character_sees
        
        print("Actions character can do:", next_character_perception.actions_character_can_do, "\n")
        actions_this_character_can_do = next_character_perception.actions_character_can_do
    
    direction = structured_director_3_llm.invoke(textwrap.dedent(
        f"""        
        You are a director directing non-player characters to follow a story.

        Look at this story:
        {state["story"]}
        
        Here are your characters:
        {state["characters"]}
        
        This is what has happened:
        {state["previous_actions"]}
        
        You are currently at this point in the story:
        {state["what_point_story_is_in"]}

        However, the story is in a video game. The character 'Player' is not a character you can direct, but instead a character controlled by a user.

        You will direct this character.
        {state['who_should_act_next']}

        Based on what point in the story you are in and what previous actions have happened, what should this character do next?

        This is what the character sees:
        {things_this_character_sees}

        This is what the character can do:
        {actions_this_character_can_do}

        Under NO CIRCUMSTANCES should a character be directed to perform an action that is not explicitly listed as a 'PossibleAction'.
        Direct the characters to follow the story as best as possible WITHIN these strict limitations.
                
        Characters CAN ONLY 'talk' to another character if 'talk' is in the list of actions they can do to a target character.
        If the desired story progression requires an action that is not allowed, the character MUST first perform a valid action (like 'walk' to the target) or the story should adapt.

        If the Player said something to a character where action isn't happening and the Player is silent after a while, bring the story back to other characters where conversations are happening.

        Character MUST speak in Tagalog (or Taglish when appropriate) if they are talking.
        
        Reply in JSON with the following structure:
        {{
            "character": "Name of character doing the action",
            "action": "Action to perform (sit, wave, talk, etc.)",
            "target": "Entity name of character or object",
            "message": "The message to say" // only include if action is "talk", otherwise omit
        }}

        Examples:
        - {{"character": "Harry",  "action": "walk", "target": "chair"}}
        - {{"character": "Harry", "action": "talk", "target": "Violet", "message": "Hello!"}}
        """
    ))

    return {"direction": direction}

def writer_adapts_story(state: State):

    print("most recent player action:", state["previous_actions"][-1], "\n")

    story = structured_writer_llm.invoke(textwrap.dedent(f"""
        You are writing a story. But it is in a video game where there is a player that is able to interact with the story.

        Here is the current story:
        {state["story"]}

        The story is set in the following setting:
        {state["setting"]}

        Here are your characters:
        {state["characters"]}

        Here is what has happened:
        {state["previous_actions"]}
        
        Determine how far into the story the current events are in. 

        This is the most recent player action:
        {state["previous_actions"][-1]}

        Given the current story and the most recent player action, adapt the story, incorporating the player's most recent action. Make major changes if need be, but if the player is generally following the story, minor to no changes are fine.
        """))

    return {"story": story}

# Initial story agent

initial_story_workflow = StateGraph(State)

initial_story_workflow.add_node("writer_makes_story", writer_makes_story)
initial_story_workflow.add_node("director_1", director_1)
initial_story_workflow.add_node("director_2", director_2)
initial_story_workflow.add_node("director_3", director_3)

initial_story_workflow.add_edge(START, "writer_makes_story")
initial_story_workflow.add_edge("writer_makes_story", "director_1")
initial_story_workflow.add_edge("director_1", "director_2")
initial_story_workflow.add_edge("director_2", "director_3")
initial_story_workflow.add_edge("director_3", END)

# Continue story agent

continue_story_workflow = StateGraph(State)

continue_story_workflow.add_node("director_1", director_1)
continue_story_workflow.add_node("director_2", director_2)
continue_story_workflow.add_node("director_3", director_3)

continue_story_workflow.add_edge(START, "director_1")
continue_story_workflow.add_edge("director_1", "director_2")
continue_story_workflow.add_edge("director_2", "director_3")
continue_story_workflow.add_edge("director_3", END)

# Disrupted story agent

disrupted_story_workflow = StateGraph(State)

disrupted_story_workflow.add_node("writer_adapts_story", writer_adapts_story)
disrupted_story_workflow.add_node("director_1", director_1)
disrupted_story_workflow.add_node("director_2", director_2)
disrupted_story_workflow.add_node("director_3", director_3)

disrupted_story_workflow.add_edge(START, "writer_adapts_story")
disrupted_story_workflow.add_edge("writer_adapts_story", "director_1")
disrupted_story_workflow.add_edge("director_1", "director_2")
disrupted_story_workflow.add_edge("director_2", "director_3")
disrupted_story_workflow.add_edge("director_3", END)

# endregion

# region FastAPI
# Fast API

from fastapi import FastAPI, WebSocket, WebSocketDisconnect, UploadFile, File
import time
import asyncio
from functools import partial
import whisper
import tempfile
import os
import torch
import edge_tts
import io
from fastapi.responses import StreamingResponse
import json

app = FastAPI()

class BeginStory(BaseModel):
    type: Literal["begin_story"]
    character_perceptions: list[CharacterPerception]

class CompletedAction(BaseModel):
    type: Literal["completed_direction", "player_interruption"]
    time: str
    character: str
    action: str
    target: str
    character_perceptions: list[CharacterPerception]
    message: Optional[str] = None  # Optional field with default None

# ------- Initialisation settings

setting = "Coffee Shop"
characters = [
    {
        "name": "Harry",
        "role": "Best Friend / Comic Relief",
        "personality": "Energetic and spontaneous with a knack for getting into ridiculous situations. He works part-time at multiple jobs and somehow manages to balance them all. He always has a story to tell and pushes the group out of their comfort zones."
    },
    {
        "name": "Emily",
        "role": "Female Friend / Potential Love Interest",
        "personality": "Practical and direct photography student who has a secret soft side. Has subtle feelings for Player but is hesitant to act on them. Values honesty above all else and can't stand when people aren't authentic."
    },
    {
        "name": "Violet",
        "role": "Female Friend / Group Mediator",
        "personality": "Warm and diplomatic literature enthusiast who loves analyzing people like they're characters in a novel. Enjoys teasing Harry and has playful arguments with him that mask their growing attraction. Often gives surprisingly insightful advice."
    },
    {
        "name": "Akira",
        "role": "Café Barista",
        "personality": "Calm and perceptive barista who seems to know exactly what everyone needs. She has an encyclopedic knowledge of coffee and tea. She offers subtle life wisdom while making drinks and notices all the relationships developing among the regulars. Stationary. Doesn't move from the front counter."
    },
    {
        "name": "Ren",
        "role": "Morning Regular with Day Job",
        "personality": "Overworked but optimistic office worker who comes to the café to escape. He has a dry sense of humor and is secretly writing a novel about office life. He provides perspective on adult responsibilities while maintaining a childlike wonder."
    },
    {
        "name": "Julia",
        "role": "Creative Regular",
        "personality": "Eccentric and passionate illustrator who treats the café as her second studio. She has synesthesia and often describes sounds in terms of colors. She unintentionally gives advice through her unique perspective on life and art. Always has paint on her clothes."
    },
    {
        "name": "Player",
        "role": "User",
        "personality": "A default vibe of 'kind-hearted but slightly awkward.' Earnest, relatable, and a little unsure of themselves, especially in romance."
    },
]

# -------

story: Optional[FiveActScene]
previous_actions: list[PreviousAction]

@app.websocket("/ws/narrative-engine")
async def narrative_engine_endpoint(websocket: WebSocket):
    await websocket.accept()

    story = None
    previous_actions = []

    try:
        while True:
            raw_data = await websocket.receive_json()
            message_type = raw_data.get("type", "")

            if message_type == "begin_story":
                begin_story = BeginStory(**raw_data)
                print("Begin story:", begin_story, "\n")

                initial_story_agent = initial_story_workflow.compile()
                response = await asyncio.to_thread(
                    partial(initial_story_agent.invoke, {
                        "story": None,
                        "what_point_story_is_in": None,
                        "who_should_act_next": None,
                        "direction": None,
                        "previous_actions": [],
                        "characters": characters,
                        "setting": setting,
                        "character_perceptions": begin_story.character_perceptions
                    })
                )
                print("Begin story agent response:", response, "\n")

                story = response["story"]

                response_data = {
                    "type": "director_response",
                    "character": response["direction"].character,
                    "action": response["direction"].action,
                    "target": response["direction"].target
                }
                if hasattr(response["direction"], "message") and response["direction"].message is not None:
                    response_data["message"] = response["direction"].message

                print("(Begin story) Director response:", response_data, "\n")

                await websocket.send_json(response_data)
                continue

            if message_type == "completed_direction":
                completed_direction = CompletedAction(**raw_data)
                print("Completed direction:", completed_direction, "\n")

                message = raw_data.get("message")

                action = PreviousAction(
                    time=completed_direction.time, 
                    character=completed_direction.character, 
                    action=completed_direction.action,
                    target=completed_direction.target,
                    message=message)
                
                previous_actions.append(action)
                print("(Completed direction) Previous actions:", previous_actions, "\n")

                if completed_direction.action == "talk" and completed_direction.target == "Player":
                    response_data = {
                        "type": "director_response",
                        "character": completed_direction.character,
                        "action": "wait_for_player",
                        "target": "Player",
                    }

                else:
                    continue_story_agent = continue_story_workflow.compile()

                    print("completed story direct:", story, "\n")

                    response = await asyncio.to_thread(
                        partial(continue_story_agent.invoke, {
                                "story": story,
                                "what_point_story_is_in": None,
                                "who_should_act_next": None,
                                "direction": None,
                                "characters": characters,
                                "previous_actions": previous_actions,
                                "setting": setting,
                                "character_perceptions": completed_direction.character_perceptions
                            })
                        )
                
                    response_data = {
                        "type": "director_response",
                        "character": response["direction"].character,
                        "action": response["direction"].action,
                        "target": response["direction"].target
                    }
                    if hasattr(response["direction"], "message") and response["direction"].message is not None:
                        response_data["message"] = response["direction"].message

                print("(Completed direction) Director response:", response_data, "\n")

                await websocket.send_json(response_data)
                continue
            
            if message_type == "player_interruption":
                player_interruption = CompletedAction(**raw_data)
                print("Player interruption:", player_interruption, "\n")

                message = raw_data.get("message")

                if player_interruption.action == "player_silence":
                    # this one is special, because the message is sent from npcs, not the player
                    # it's called an interruption because it affects the story
                    action = PreviousAction(
                        time=player_interruption.time, 
                        character="Player", 
                        action="player_silence",
                        target=player_interruption.character,
                        message=message)
                else:
                    action = PreviousAction(
                        time=player_interruption.time, 
                        character=player_interruption.character, 
                        action=player_interruption.action,
                        target=player_interruption.target,
                        message=message)
                    
                previous_actions.append(action)
                print("(Player interruption) Previous actions:", previous_actions, "\n")

                disrupted_story_agent = disrupted_story_workflow.compile()
                response = await asyncio.to_thread(
                    partial(disrupted_story_agent.invoke, {
                        "story": story,
                        "what_point_story_is_in": None,
                        "who_should_act_next": None,
                        "direction": None,
                        "characters": characters,
                        "previous_actions": previous_actions,
                        "setting": setting,
                        "character_perceptions": player_interruption.character_perceptions
                    })
                )
                print("(Player interruption) Disrupted story agent response:", response, "\n")

                response_data = {
                    "type": "director_response",
                    "character": response["direction"].character,
                    "action": response["direction"].action,
                    "target": response["direction"].target
                }
                if hasattr(response["direction"], "message") and response["direction"].message is not None:
                    response_data["message"] = response["direction"].message

                print("(Player interruption) Director response:", response_data, "\n")

                await websocket.send_json(response_data)
                continue
            
            if message_type == "heartbeat":
                await websocket.send_json({
                    "type": "heartbeat_ack", 
                    "timestamp": time.time()
                })
                continue

            else:
                print("Unknown message type:", message_type)
                continue
    
    except WebSocketDisconnect:
        print("Client disconnected")

device = "cuda" if torch.cuda.is_available() else "cpu"
whisper_model = whisper.load_model("base", download_root="E:\Code\AppData\whisper").to(device)

class TranscribeResponse(BaseModel):
    transcription: str

@app.post("/transcribe")
async def transcribe_audio(audio: UploadFile = File(...)) -> TranscribeResponse:
    temp_name = None
    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as temp_audio:
        content = await audio.read()
        temp_audio.write(content)
        temp_audio.flush()
        temp_name = temp_audio.name
    
    result = whisper_model.transcribe(temp_name)

    print("Transcription:", result["text"], "\n")
    
    os.unlink(temp_name)
    
    return TranscribeResponse(transcription=str(result["text"]))

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

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("DirectorBrain:app", host="127.0.0.1", port=8000, reload=True)

# endregion
