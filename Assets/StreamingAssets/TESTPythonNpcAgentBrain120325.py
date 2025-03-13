

# check goal
# think what to do next
# do thing

import asyncio
import textwrap
import time
from typing_extensions import TypedDict
from typing import Literal, Union
from pydantic import BaseModel
from langchain_google_genai import ChatGoogleGenerativeAI
from langgraph.graph import StateGraph, START, END

llm = ChatGoogleGenerativeAI(
    model="gemini-2.0-flash-lite",
    google_api_key="AIzaSyBbgaNd1O9xLJK1uai_i8fwGwgMDMRwPjA",
)

class Walk(BaseModel):
    action: Literal["walk"]
    target: str

class Talk(BaseModel):
    action: Literal["talk"]
    target: str
    message: str

class Interact(BaseModel):
    action: Literal["interact"]
    target: str

DecisionStructure = Union[Walk, Interact, Talk]

class PerceivedState(BaseModel):
    objects_you_can_walk_to: list[str]
    objects_you_can_interact_with: list[str]
    characters_you_can_walk_to: list[str]
    characters_you_can_interact_with: list[str]

class State(TypedDict):
    goal: str
    perceived_state: PerceivedState
    decision: DecisionStructure

structured_decision_llm = llm.with_structured_output(DecisionStructure)

def think_what_to_do_next(state: State):
    npc_name = "Eugene"
    npc_personality = "Eugene Fitzherbert, also known as Flynn Rider, is a charming and charismatic thief with a complex personality that blends confidence and wit with vulnerability and emotional depth, hiding a troubled past beneath his smooth-talking, sarcastic, and adventurous exterior."

    prompt = textwrap.dedent(f"""
        You are {npc_name}, an NPC in a virtual world. {npc_personality}
        
        You can perceive the world around you and take actions based on what you see.
        You can walk to objects and characters, interact with objects, and speak to characters.

        Your current perception will be provided to you.
        Think about what you want to do based on your personality and current situation.
        You can walk, talk, and interact with things in the virtual world.

        Always act in character and according to your personality.

        This is your current goal: {state['goal']}
        This is what you see around you: {state['perceived_state']}

        To walk to a location: {{"action": "walk", "args": "location"}}
        To interact with an object: {{"action": "interact", "args": "object"}}
        To talk to someone: {{"action": "talk", "target": "person", "message": "text"}}

        For example:
        {{"action": "walk", "target": "Chair"}}
        {{"action": "interact", "target": "Coffee machine"}}
        {{"action": "talk", "target": "Jeff", "message": "Hello there."}}

        Now, choose an action and respond with a JSON object in the correct format.
        """)

    decision = structured_decision_llm.invoke(prompt)
    return {"decision": decision}

async def walk(state: State):
    target = state["decision"].target
    perceived = state["perceived_state"]
    print(f"Walking to {target}")
    # Update state: move target from walkable to interactable if it’s a character
    if target in perceived["characters_you_can_walk_to"]:
        perceived["characters_you_can_walk_to"].remove(target)
        perceived["characters_you_can_interact_with"].append(target)
    elif target in perceived["objects_you_can_walk_to"]:
        perceived["objects_you_can_walk_to"].remove(target)
        perceived["objects_you_can_interact_with"].append(target)

    time.sleep(3)
    
    return {"perceived_state": perceived}

async def talk(state: State):
    target = state["decision"].target
    message = state["decision"].message
    print(f"Talking to {target}: {message}")
    # No state change here, but you could add logic (e.g., mark conversation as done)

    time.sleep(3)

    return {}

async def interact(state: State):
    target = state["decision"].target
    print(f"Interacting with {target}")
    # Could add state changes (e.g., picking up an item)
    
    time.sleep(3)

    return {}

async def do_thing(state: State):
    decision = state["decision"]

    if decision.action == "walk":
        await walk(state)
    if decision.action == "talk":
        await talk(state)
    if decision.action == "interact":
        await interact(state)
    
    return

workflow = StateGraph(State)

workflow.add_node("think_what_to_do_next", think_what_to_do_next)
workflow.add_node("do_thing", do_thing)

workflow.add_edge(START, "think_what_to_do_next")
workflow.add_edge("think_what_to_do_next", "do_thing")
workflow.add_edge("do_thing", END)

agent = workflow.compile()

async def run_agent():
    state = await agent.ainvoke({
            "goal": "Have a friendly conversation with Gerard.",
            "perceived_state": {
                "objects_you_can_walk_to": [],
                "objects_you_can_interact_with": [],
                "characters_you_can_walk_to": ["Felix", "Diana", "Gerard"],
                "characters_you_can_interact_with": []
            }
        })
    
    print(state)

asyncio.run(run_agent())