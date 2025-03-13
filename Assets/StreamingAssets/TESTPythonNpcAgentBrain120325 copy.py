

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

class Idle(BaseModel):
    action: Literal["idle"]

DecisionStructure = Union[Walk, Interact, Talk, Idle]

class PerceivedState(BaseModel):
    objects_you_can_walk_to: list[str]
    objects_you_can_interact_with: list[str]
    characters_you_can_walk_to: list[str]
    characters_you_can_interact_with: list[str]
    characters_you_can_talk_to: list[str]

# class ConversationState
    # whenever engage in a conversation with someone
    # start new conversation list
    # when you tell then something, it shows on your list as 'you'
    # when they tell you something, it shows on your list as 'name'
    # this is the same for them. both parties.
    # they use this list to know state and talk to each other.
    # when conversation is done, list is stored in 'previous conversations list'. hopefully this is all thats needed so llm knows what was a previous conversation and what isnt.
    # when interrupted, stop. though this part is hard and annoying. so maybe, no interruptions yet.

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
        {{"action": "idle"}}

        Now, choose an action and respond with a JSON object in the correct format.
        """)

    decision = structured_decision_llm.invoke(prompt)
    return {"decision": decision}

async def walk(state: State):
    # nothing yet
    return

async def talk(state: State):
    # nothing yet 
    return

async def interact(state: State):
    # nothing yet
    return

async def idle(state: State):
    # nothing yet
    return

async def do_thing(state: State):
    decision = state["decision"]

    if decision.action == "walk":
        await walk(state)
    if decision.action == "talk":
        await talk(state)
    if decision.action == "interact":
        await interact(state)
    if decision.action == "idle":
        await idle(state)
    
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
                "characters_you_can_interact_with": [],
                "characters_you_can_talk_to": []
            }
        })
    
    print(state)

asyncio.run(run_agent())

# --------------
# --------------

# server opens
# server waits
# client connects to websocket
# client tells websocket 'here's my name. here's my personality. here's data for my current perceived_state. what should i do?' (goal is set here, in server)
# server sees it, does the agent invoke thing then sends its data back through websocket (using walk(), talk(), etc).
# client sees that server told it to do something. it does it. then tells server its new perception.
# server notices the new perception. it updates the perception for that websocket in ther server. it invokes the agent again with the new perception. the agent is in some sort of infinte loop (the agent will tell the client to do something again).
# there's a client/server pingpong heartbeat in here, somewhere