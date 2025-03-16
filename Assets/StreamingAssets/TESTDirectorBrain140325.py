# writer makes a scene
# director follows scene

import textwrap
from typing_extensions import TypedDict
from typing import Literal, Union
from pydantic import BaseModel
from langchain_google_genai import ChatGoogleGenerativeAI
from langgraph.graph import StateGraph, START, END

llm = ChatGoogleGenerativeAI(
    model="gemini-2.0-flash-lite",
    google_api_key="AIzaSyBbgaNd1O9xLJK1uai_i8fwGwgMDMRwPjA",
)

from pydantic import BaseModel, Field

# ----- Director BaseModels

class SimpleAction(BaseModel):
    character: str = Field(description="Name of the character that will do the action")
    action: str = Field(description="The action to perform (e.g., sit, wave)")
    target: str = Field(description="The target of the action (character name or object)")

class TalkAction(BaseModel):
    character: str = Field(description="Name of the character that will do the action")
    action: Literal["talk"] = Field(description="The action to perform, must be 'talk'")
    target: str = Field(description="The target of the action (character name or object)")
    message: str = Field(description="Message content for talking")

DirectedAction = Union[SimpleAction, TalkAction]

# ----- Writer BaseModels

class SceneAct(BaseModel):
    content: str = Field(description="The actual content of this section of the scene")
    purpose: str = Field(description="Narrative function this section serves")
    duration: str = Field(description="Relative length/pacing of this section (brief, extended, etc.)")

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

class CharacterPerception(BaseModel):
    character: str
    things_character_sees: list[str]
    actions_character_can_do: list[str]

class State(TypedDict):
    story: FiveActScene
    direction: DirectedAction
    previous_actions: list[DirectedAction]
    characters: list[Character]
    character_perceptions: list[CharacterPerception]
    setting: str

# ----- Graph Nodes

structured_writer_llm = llm.with_structured_output(FiveActScene)

def writer_makes_story(state: State):
    story = structured_writer_llm.invoke(textwrap.dedent(f"""
        Write a scene for a romantic slice of life anime.

        Here are your characters.
        {state["characters"]}               

        The story is set in the following setting:
        {state["setting"]}
        """))

    return {"story": story}

structured_director_llm = llm.with_structured_output(DirectedAction)

def director_follows_scene(state: State):
    direction = structured_director_llm.invoke(textwrap.dedent(f"""
        Look at this story:
        {state["story"]}

        The story is set in the following setting:
        {state["setting"]}
        
        Here are your characters:
        {state["characters"]}
        
        This is what has happened:
        {state["previous_actions"]}
        
        This is what each character sees and is able to do.
        {state["character_perceptions"]}

        Direct the characters to follow the story as best as possible given their limitations.

        Look at what has happened. Judge what part of the story we are in. Think what should happen next. 
        According to what should happen next, who should act next and what should they do?

        Reply in JSON. Use one of these structures:
        - For actions without a message (e.g., sit, wave):
        {{
            "character": "Name of character doing the action",
            "action": "Action to perform (sit, wave, etc.)"
            "target": "Name of character or object",
        }}

        - For talk, specifically:
        {{
            "character": "Name of character doing the action",
            "action": "talk",
            "target": "Name of character or object",
            "message": "The message to say"
        }}

        Examples:
        - {{"character": "Haruto",  "action": "sit", "target": "chair"}}
        - {{"character": "Haruto", "action": "talk", "target": "Aiko", "message": "Hello!"}}
        """))

    return {"direction": direction}

def writer_adapts_story(state: State):
    story = structured_writer_llm.invoke(textwrap.dedent(f"""
        You are writing a story. But it is in a video game where there is a player that is able to interact with the story.

        Here is the current story:
        {state["story"].story}

        The story is set in the following setting:
        {state["setting"]}

        Here are your characters:
        {state["characters"]}

        Here is what has happened:
        {state["previous_actions"]}

        This is the most recent player action:
        {state["previous_actions"][-1]}

        Given the current story and the most recent player action, adapt the story, incorporating the player's most recent action. Make major changes if need be, but if the player is generally following the story, minor to no changes are fine.
        """))

    return {"story": story}

# Initial story agent

initial_story_workflow = StateGraph(State)

initial_story_workflow.add_node("writer_makes_story", writer_makes_story)
initial_story_workflow.add_node("director_follows_scene", director_follows_scene)

initial_story_workflow.add_edge(START, "writer_makes_story")
initial_story_workflow.add_edge("writer_makes_story", "director_follows_scene")
initial_story_workflow.add_edge("director_follows_scene", END)

# Continue story agent

continue_story_workflow = StateGraph(State)

continue_story_workflow.add_node("director_follows_scene", director_follows_scene)

continue_story_workflow.add_edge(START, "director_follows_scene")
continue_story_workflow.add_edge("director_follows_scene", END)

# Disrupted story agent

disrupted_story_workflow = StateGraph(State)

disrupted_story_workflow.add_node("writer_adapts_story", writer_adapts_story)
disrupted_story_workflow.add_node("director_follows_scene", director_follows_scene)

disrupted_story_workflow.add_edge(START, "writer_adapts_story")
disrupted_story_workflow.add_edge("writer_adapts_story", "director_follows_scene")
disrupted_story_workflow.add_edge("director_follows_scene", END)

# Fast API

from fastapi import FastAPI, WebSocket, WebSocketDisconnect
import time
import asyncio
from functools import partial

app = FastAPI()

class RawMessage(BaseModel):
    type: str

class BeginStory(BaseModel):
    type: Literal["begin_story"]
    character_perceptions: list[CharacterPerception]

class CompletedAction(BaseModel):
    type: Literal["completed_direction", "player_interruption"]
    time: str
    character: str
    action: str
    character_perceptions: list[CharacterPerception]

class PreviousAction(BaseModel):
    time: str
    character: str
    action: str

# ------- Initialisation settings

setting = "Coffee Shop"
characters = [
    {
        "name": "Player",
        "role": "Protagonist",
        "personality": "A default vibe of 'kind-hearted but slightly awkward.' Earnest, relatable, and a little unsure of themselves, especially in romance."
    },
    {
        "name": "Haruto",
        "role": "Best Friend / Comic Relief",
        "personality": "Cheerful and mischievous. Energetic wingman who pushes the Player out of their comfort zone. A prankster with terrible-but-hilarious dating advice, yet fiercely loyal with surprising moments of wisdom."
    },
    {
        "name": "Aiko",
        "role": "Main Love Interest",
        "personality": "Gentle yet determined. Soft-spoken with a warm smile, passionate about a niche hobby like painting or gardening. Quietly strong, shy about her feelings, but shows affection through subtle actions like gifts or blushing."
    },
    {
        "name": "Sakura",
        "role": "Confidante / Matchmaker",
        "personality": "Sassy and perceptive. Bold and quick-witted, loves teasing others and sees through emotions before anyone else. Acts aloof about romance but secretly roots for the Player and Aiko, hiding a caring heart behind her sharp tongue."
    }
]

# -------

previous_actions: list[PreviousAction] = []

@app.websocket("/narrative-engine")
async def websocket_endpoint(websocket: WebSocket):
    await websocket.accept()

    try:
        while True:
            raw_data = await websocket.receive_json()
            raw_message = RawMessage(**raw_data)

            if raw_message.type == "begin_story":
                begin_story = BeginStory(**raw_message)

                initial_story_agent = initial_story_workflow.compile()
                response = await asyncio.to_thread(
                    partial(initial_story_agent.invoke, {
                        "characters": characters,
                        "setting": setting,
                        "character_perceptions": begin_story.character_perceptions
                    })
                )

                response_data = {
                    "type": "director_response",
                    "character": response["direction"].character,
                    "action": response["direction"].action,
                    "target": response["direction"].target
                }
                if isinstance(response["direction"], TalkAction):
                    response_data["message"] = response["direction"].message

                await websocket.send_json(response_data)
                continue

            if raw_message.type == "completed_direction":
                completed_direction = CompletedAction(**raw_message)
                action = PreviousAction(time=completed_direction.time, character=completed_direction.character, action=completed_direction.action)
                previous_actions.append(action)

                continue_story_agent = continue_story_workflow.compile()
                response = await asyncio.to_thread(
                    partial(continue_story_agent.invoke, {
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
                if isinstance(response["direction"], TalkAction):
                    response_data["message"] = response["direction"].message

                await websocket.send_json(response_data)
                continue
            
            if raw_message.type == "player_interruption":
                player_interruption = CompletedAction(**raw_message)
                action = PreviousAction(time=player_interruption.time, character=player_interruption.character, action=player_interruption.action)
                previous_actions.append(action)

                disrupted_story_agent = disrupted_story_workflow.compile()
                response = await asyncio.to_thread(
                    partial(disrupted_story_agent.invoke, {
                        "characters": characters,
                        "previous_actions": previous_actions,
                        "setting": setting,
                        "character_perceptions": player_interruption.character_perceptions
                    })
                )
                
                response_data = {
                    "type": "director_response",
                    "character": response["direction"].character,
                    "action": response["direction"].action,
                    "target": response["direction"].target
                }
                if isinstance(response["direction"], TalkAction):
                    response_data["message"] = response["direction"].message

                await websocket.send_json(response_data)
                continue
            
            if raw_message.type == "heartbeat":
                await websocket.send_json({
                    "type": "heartbeat_ack", 
                    "timestamp": time.time()
                })
                continue
    
    except WebSocketDisconnect:
        print("Client disconnected")

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("TESTDirectorBrain140325:app", host="127.0.0.1", port=8000, reload=True)

