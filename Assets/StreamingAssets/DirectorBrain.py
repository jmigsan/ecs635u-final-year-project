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
from dotenv import load_dotenv

load_dotenv()

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
    time: int
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
        Write an outline for a scene in a romantic slice-of-life anime, designed for a video game. In this game, one character is the user, referred to as the "Player". However, the story should unfold independently of the Player, who will only serve as a passive observer.

        Below are the characters available:
        {state["characters"]}

        The scene is set in:
        {state["setting"]}

        Guidelines:
        - The narrative should highlight a gentle, romantic atmosphere typical of slice-of-life anime.
        - The Player does not influence the story; they simply observe the events.
        - The Player is still part of the story. The Player is seen by other characters and has relationships with them. 
        - In the video game context, characters are restricted to only "talk" and "walk" actions.
        - Do not include any actions that require additional interactions (e.g., "spill coffee", "jump", or "wave").

        Create an engaging and thoughtful outline that respects the setting and constraints.
        """
    ))


    return {"story": story}

# def director_1(state: State):

#     response = llm.invoke(textwrap.dedent(
#         f"""
#         Look at this story:
#         {state["story"]}

#         The story is set in the following setting:
#         {state["setting"]}
        
#         Here are your characters:
#         {state["characters"]}
        
#         This is what has happened:
#         {state["previous_actions"]}

#         Based on the previous actions, determine at what point the story is in.
#         """
#     ))

#     print("director 1 said:", response, "\n")
#     return {"what_point_story_is_in": response.content}

structured_director_2_llm = llm.with_structured_output(WhoNext)

def director_2(state: State):

    response = structured_director_2_llm.invoke(textwrap.dedent(
    f"""
        You are a director guiding non-player characters (NPCs) to follow the story's progression.

        Current Context:
        ----------------
        Story:
        {state["story"]}

        Characters:
        {state["characters"]}

        Recent Narrative Actions:
        {state["previous_actions"]}

        IMPORTANT:
        - The "Player" is controlled by a user and should not be directed.

        Instructions:
        1. Review the most recent action:
        {state["previous_actions"][-1]}
        
        2. Determine the current narrative state based on the provided context.

        3. Decide which character should act next. Remember:
        - If one character has just walked up to another, the next action must involve dialogue.
        - If a character asks another a question, the responding character must answer immediately.
        - If the Player interacts but then remains silent, shift the narrative focus to conversations among other characters.
        - If a character has been conversing with the Player repeatedly without a response, involve a different character to maintain momentum.
        - If a character has been conversing with another character about something and it looks like the conversation is going in circles, advance the story by bringing up another topic or another character.
        - If a conversation has been interrupted by the Player and it seems like the Player's conversation is done, go back to the previously ongoing conversation.

        Output:
        Who should act next?
        """
    ))


    print(response)

    return {"who_should_act_next": response}

structured_director_3_llm = llm.with_structured_output(DirectedAction)

def director_3(state: State):
    next_character_perception = None
    for perception in state["character_perceptions"]:
        if perception.character == state["who_should_act_next"].character:
            next_character_perception = perception
            break

    things_this_character_sees = []
    actions_this_character_can_do = []
    
    if next_character_perception is not None:
        print("Things character sees:", next_character_perception.things_character_sees, "\n")
        things_this_character_sees = next_character_perception.things_character_sees
        
        print("Actions character can do:", next_character_perception.actions_character_can_do, "\n")
        actions_this_character_can_do = next_character_perception.actions_character_can_do
    
    prompt = textwrap.dedent(
        f"""--- Director Instructions—Rules & Fallback Procedures ---

        1. PERMITTED ACTIONS ONLY:
        - A character may only perform an action explicitly listed in actions_this_character_can_do.
        - Characters can only "talk" to another character if "talk" appears as an allowed action on that target.

        2. FALLBACK BEHAVIOR:
        - If an intended action is not permitted:
            - Example: If the narrative requires a "talk" action but "talk" is not in the allowed actions, then first instruct the character to perform a valid precursor action (e.g., "walk") toward the intended target.
            - Once the precursor action establishes the proper narrative context (e.g., approaching the other character), call for the conversational action if it becomes available on a subsequent turn.

        3. SITUATION-SPECIFIC GUIDELINES:
        - If someone has just walked up to someone, the next action must be dialogue—someone must say something.
        - If one character asks another something, the other character must respond in their next turn.
        - If the Player (a user-controlled character) is silent after speaking or no clear narrative action is occurring, direct the story to involve conversation among other characters to keep momentum.
        - Additionally, if a character has been talking to the Player more than once without a response from the Player in between, use another character to advance the narrative.
        - If a character has been conversing with another character about something and it looks like the conversation is going in circles, advance the story by bringing up another topic or another character.


        4. OVERVIEW OF CONTEXT:
        - Story: {state["story"]}
        - Characters Available: {state["characters"]}
        - Previous Actions: {state["previous_actions"]}
        - CURRENT ACTOR: {state['who_should_act_next'].character}
            Sees: {things_this_character_sees}
        - ALLOWED ACTIONS: {actions_this_character_can_do}

        5. DIRECTOR TASK:
        - You are directing non-player characters (NPCs) to follow the story's progression within the above constraints.
        - Remember: the "Player" is controlled by a user and should NOT be directed.
        - Determine what {state['who_should_act_next'].character} should do next based on the story context, current observations, and their allowed actions.
        - CRITICAL CHECK: Before instructing an action, verify that it is listed as an allowed "PossibleAction". If the natural story progression requires an action that is not allowed (e.g., "talk"), then first direct a permitted action (like "walk") that will logically set up the intended narrative and enable the eventual transition into the desired action.

        YOUR TASK:
        Direct {state['who_should_act_next'].character} on their next move. Ensure that if a conversation is required or expected by the narrative, proper steps (like moving closer or initiating a precursor action) are taken if "talk" is not currently permitted.

        --- End of Director Prompt ---"""
    )

        # 4. LANGUAGE REQUIREMENT:
        # - When characters speak (using any dialogue action), they MUST do so in Tagalog (or Taglish when appropriate).

        #5.directortask
        # Remember: All dialogue must be in Tagalog (or Taglish).
        
    # print("Director 3 prompt:", prompt, "\n")

    direction = structured_director_3_llm.invoke(prompt)

    return {"direction": direction}

    # I had this before, but then it wrote json for the message once. maybe it writes json like that because i told it to,
    # Reply in JSON with the following structure:
    # {{
    #     "character": "Name of character doing the action",
    #     "action": "Action to perform (sit, wave, talk, etc.)",
    #     "target": "Entity name of character or object",
    #     "message": "The message to say" // only include if action is "talk", otherwise omit
    # }}

    # Examples:
    # - {{"character": "Harry",  "action": "walk", "target": "chair"}}
    # - {{"character": "Harry", "action": "talk", "target": "Violet", "message": "Hello!"}}

def writer_adapts_story(state: State):

    print("most recent player action:", state["previous_actions"][-1], "\n")

    story = structured_writer_llm.invoke(textwrap.dedent(
        f"""
        You are tasked with updating the narrative of a video game. In this game, the story unfolds on its own until the Player (controlled by the user) interjects. When the Player acts, their input should be integrated into the narrative, altering the story's course accordingly.

        --- Current Context ---
        Story:
        {state["story"]}

        Setting:
        {state["setting"]}

        Characters:
        {state["characters"]}

        Narrative History:
        {state["previous_actions"]}

        --- Recent Player Interaction ---
        Most Recent Player Action:
        {state["previous_actions"][-1]}

        --- Instructions ---
        1. Assess how far along the narrative currently is.
        2. Adapt the story to incorporate the Player's latest action:
        - If the action is disruptive or significantly different from the ongoing narrative, introduce substantial changes.
        - If the Player's input aligns reasonably with the existing story, make only minor adjustments.
        3. Even though the Player generally isn't part of the story, treat their recent action as a catalyst to alter or steer the narrative going forward.
        4. Ensure the updated narrative remains coherent, engaging, and true to the game's setting and tone.

        Output the updated story outline that reflects these changes.
        """
    ))



    return {"story": story}

# Initial story agent

initial_story_workflow = StateGraph(State)

initial_story_workflow.add_node("writer_makes_story", writer_makes_story)
initial_story_workflow.add_node("director_2", director_2)
initial_story_workflow.add_node("director_3", director_3)

initial_story_workflow.add_edge(START, "writer_makes_story")
initial_story_workflow.add_edge("writer_makes_story", "director_2")
initial_story_workflow.add_edge("director_2", "director_3")
initial_story_workflow.add_edge("director_3", END)

# Continue story agent

continue_story_workflow = StateGraph(State)

continue_story_workflow.add_node("director_2", director_2)
continue_story_workflow.add_node("director_3", director_3)

continue_story_workflow.add_edge(START, "director_2")
continue_story_workflow.add_edge("director_2", "director_3")
continue_story_workflow.add_edge("director_3", END)

# Disrupted story agent

disrupted_story_workflow = StateGraph(State)

disrupted_story_workflow.add_node("writer_adapts_story", writer_adapts_story)
disrupted_story_workflow.add_node("director_2", director_2)
disrupted_story_workflow.add_node("director_3", director_3)

disrupted_story_workflow.add_edge(START, "writer_adapts_story")
disrupted_story_workflow.add_edge("writer_adapts_story", "director_2")
disrupted_story_workflow.add_edge("director_2", "director_3")
disrupted_story_workflow.add_edge("director_3", END)

# endregion

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
        "personality": "Calm and perceptive barista who seems to know exactly what everyone needs. She has an encyclopedic knowledge of coffee and tea. She offers subtle life wisdom while making drinks and notices all the relationships developing among the regulars."
    },
    {
        "name": "Player",
        "role": "User",
        "personality": "A default vibe of 'kind-hearted but slightly awkward.' Earnest, relatable, and a little unsure of themselves, especially in romance."
    },
]

# -------

@app.websocket("/ws/narrative-engine")
async def narrative_engine_endpoint(websocket: WebSocket):
    await websocket.accept()

    story: Optional[FiveActScene] = None
    previous_actions: list[PreviousAction] = [
        PreviousAction(
            time=0,
            character="System",
            action="start_story",
            target="All",
            message="The story has started."
        )
    ]
    potential_directions: list[DirectedAction] = []

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
                        "who_should_act_next": None,
                        "direction": None,
                        "previous_actions": previous_actions,
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

                if response["direction"].action == "talk":
                    potential_directions.append(await queue_potential_direction(
                        story,
                        previous_actions,
                        begin_story.character_perceptions,
                        response_data["character"],
                        response_data["action"],
                        response_data["target"],
                        response_data["message"]
                    ))

                await websocket.send_json(response_data)
                continue

            if message_type == "completed_direction":
                completed_direction = CompletedAction(**raw_data)
                print("Completed direction:", completed_direction, "\n")

                message = raw_data.get("message")

                action = PreviousAction(
                    time=previous_actions[-1].time + 1, 
                    character=completed_direction.character, 
                    action=completed_direction.action,
                    target=completed_direction.target,
                    message=message)
                
                previous_actions.append(action)
                print("(Completed direction) Previous actions:", previous_actions, "\n")

                if potential_directions:
                    preloaded_direction = potential_directions.pop(0)
                    response_data = {
                        "type": "director_response",
                        "character": preloaded_direction.character,
                        "action": preloaded_direction.action,
                        "target": preloaded_direction.target
                    }
                    if hasattr(preloaded_direction, "message") and preloaded_direction.message is not None:
                        response_data["message"] = preloaded_direction.message

                elif completed_direction.action == "talk" and completed_direction.target == "Player":
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

                if response_data["action"] == "talk":
                    potential_directions.append(await queue_potential_direction(
                        story,
                        previous_actions,
                        completed_direction.character_perceptions,
                        response_data["character"],
                        response_data["action"],
                        response_data["target"],
                        response_data["message"]
                    ))

                await websocket.send_json(response_data)
                continue
            
            if message_type == "player_interruption":
                player_interruption = CompletedAction(**raw_data)
                print("Player interruption:", player_interruption, "\n")

                potential_directions = []

                message = raw_data.get("message")

                if player_interruption.action == "player_silence":
                    # this one is special, because the message is sent from npcs, not the player
                    # it's called an interruption because it affects the story
                    action = PreviousAction(
                        time=previous_actions[-1].time + 1, 
                        character="Player", 
                        action="player_silence",
                        target=player_interruption.character,
                        message=message)
                else:
                    action = PreviousAction(
                        time=previous_actions[-1].time + 1, 
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

                if response["direction"].action == "talk":
                    potential_directions.append(await queue_potential_direction(
                        story,
                        previous_actions,
                        player_interruption.character_perceptions,
                        response_data["character"],
                        response_data["action"],
                        response_data["target"],
                        response_data["message"]
                    ))

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

async def queue_potential_direction(story: Optional[FiveActScene], previous_actions: list[PreviousAction], character_perceptions: list[CharacterPerception], character: str, action: str, target: str, message: Optional[str] = None):
    temp_previous_actions = previous_actions.copy()

    optimistic_previous_action = PreviousAction(
        time=temp_previous_actions[-1].time + 1, 
        character=character, 
        action=action,
        target=target,
        message=message)
    
    temp_previous_actions.append(optimistic_previous_action)
    print("(Potential direction) Previous actions:", temp_previous_actions, "\n")

    optimistic_direction: DirectedAction

    if action == "talk" and target == "Player":
        optimistic_direction = DirectedAction(
            character=character,
            action="wait_for_player",
            target="Player",
            message=None
        )
    else:
        continue_story_agent = continue_story_workflow.compile()

        response = await asyncio.to_thread(
            partial(continue_story_agent.invoke, {
                "story": story,
                "who_should_act_next": None,
                "direction": None,
                "characters": characters,
                "previous_actions": temp_previous_actions,
                "setting": setting,
                "character_perceptions": character_perceptions
            })
        )

        directed_message = None
        if hasattr(response["direction"], "message") and response["direction"].message is not None:
            directed_message = response["direction"].message
        
        optimistic_direction =  DirectedAction(
            character=response["direction"].character,
            action=response["direction"].action,
            target=response["direction"].target,
            message=directed_message
        )

    print("(Potential direction) Director response:", optimistic_direction, "\n")

    return optimistic_direction

# endregion

# region Whisper

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
        # "language": "tagalog",
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
