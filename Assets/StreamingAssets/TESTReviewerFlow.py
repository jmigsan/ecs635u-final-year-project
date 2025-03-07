from typing import TypedDict
import instructor
import google.generativeai as genai
from langgraph.graph import StateGraph
from pydantic import BaseModel
from langchain_google_genai import ChatGoogleGenerativeAI

class NpcDirections(BaseModel):
    npc_name: str
    direction: str

class Directions(BaseModel):
    music: str
    weather: str
    npc_directions: list[NpcDirections] 

class State(TypedDict):
    game_state: str
    story: str
    current_act: int
    current_scene: int
    review_decision: str
    directions: Directions

genai.configure(api_key="AIzaSyBbgaNd1O9xLJK1uai_i8fwGwgMDMRwPjA")

client = instructor.from_gemini(
    client=genai.GenerativeModel(model_name="gemini-2.0-flash"),
    mode=instructor.Mode.GEMINI_JSON
)

llm = ChatGoogleGenerativeAI(
    model="gemini-2.0-flash",
    google_api_key="AIzaSyBbgaNd1O9xLJK1uai_i8fwGwgMDMRwPjA",
)

def reviewer(state: State) -> State:
    prompt = f"""
        You are reviewing these events.
        {state['game_state']}
        Look at this story
        {state['story']}
        You are currently in Act {state['current_act']} Scene {state['current_scene']}
        If the events are following the story, respond with 'Aligned'
        If the events have a minor deviation from the story, respond with 'Minor'
        If the events have a major deviation from the story, respond with 'Major'
        If the outcome of Act {state['current_act']} Scene {state['current_scene']} has been completed, respond with 'Next'
        """

    response = llm.invoke(prompt)
    decision = response.content.strip().lower()
    state['review_decision'] = decision
    return state

def major_disruption_writer(state: State) -> State:
    prompt = f"""
        The previous story was disrupted. The player has taken actions that significantly changed the story direction.
        Previous story: {state['story']}
        Current act: {state['current_act']}, Current scene: {state['current_scene']}
        Events: {state['game_state']}
        
        Create a new story from Act {state['current_act']}, Scene {state['current_scene']} that incorporates these changes naturally.
        """

    response = llm.invoke(prompt)
    state['story'] = response.content.strip()
    return state

def director(state: State) -> State:
    prompt = f"""
        You are directing video game NPCs.
        You are making them follow a story.
        Here is the story you are following: 
        {state["story"]}
        
        You are currently in Act: {state["current_act"]}, Scene: {state["current_scene"]}.
        Here is the current state of the story: 
        {state["game_state"]}
        
        For any relevant character, provide specific commands to advance the story.
    """

    structured_response = client.chat.completions.create(
        messages=[{"role": "user", "content": prompt}],
        response_model=Directions
    )

    state["directions"] = structured_response
    
    return state

graph = StateGraph(State)