

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

from fastapi import FastAPI, WebSocket, WebSocketDisconnect

class NPC:
    def __init__(self, name: str, personality: str, perception: PerceivedState, websocket: WebSocket):
        self.name = name
        self.personality = personality
        self.perception = perception
        self.websocket = websocket
        self.goal = None

class RawMessage(BaseModel):
    type: str

class InitialiseClientMessage(BaseModel):
    type: Literal["initialise_client"]
    name: str
    personality: str
    perceived_state: PerceivedState

app = FastAPI()

active_npcs = {}

class StoryManager:
    def __init__(self):
        self.story = ""

story_manager = StoryManager()

@app.websocket("/npc")
async def npc_endpoint(websocket: WebSocket):
    await websocket.accept()

    try:
        while True:
            raw_data = await websocket.receive_json()
            raw_message = RawMessage(**raw_data)

            if raw_message.type == "initialise_client":
                message = InitialiseClientMessage(**raw_message)
                active_npcs[message.name] = NPC(name=message.name, personality=message.personality, perception=message.perceived_state, websocket=websocket)

    except WebSocketDisconnect:
        print("Client disconnected")

def writer_make_story():
    story_manager.story = textwrap.dedent("""
        # Coffee Shop Romance: Slice of Life Anime Scene Outline

        ## Setting
        - Cozy cafe in the afternoon
        - Soft jazz playing in background
        - Warm sunlight streaming through windows

        ## Characters
        - MC: Main character, slightly disheveled after a long day of classes
        - MC's Best Friend: Already at the cafe, enthusiastic and teasing
        - Female Lead: Shy but kind, notices MC immediately
        - Female Lead's Best Friend: Observant and supportive wingwoman

        ## Scene Progression

        1. Initial Setup
        - Female Lead and her Best Friend are sitting at a corner table
        - They're looking at something on Female Lead's phone, giggling
        - MC's Best Friend is already seated at another table

        2. MC's Entrance
        - Bell chimes as MC enters the cafe
        - Brief eye contact between MC and Female Lead
        - Female Lead quickly looks away, blushing
        - MC's Best Friend calls out and waves to MC

        3. The Awkward Incident
        - MC walks toward his friend, not seeing a puddle on the floor
        - MC slips
        - Female Lead notices and gasps in concern
        - Everyone in the cafe turns to look at the commotion

        4. Embarrassment & Recognition
        - Female Lead's Best Friend recognizes MC from Female Lead's literature class
        - Female Lead is embarrassed by her friend's comment
        - MC's Best Friend goes to help while teasing MC
        - MC is mortified by the attention

        5. The Meet-Cute
        - Female Lead approaches with napkins to help
        - MC and Female Lead make meaningful eye contact
        - They share a moment of connection

        6. Breaking the Ice
        - Female Lead shares her own embarrassing story about spilling coffee
        - They laugh together, easing the tension
        - MC's Best Friend invites Female Lead and her friend to join them
        - Female Lead's Best Friend encourages her with a thumbs-up

        7. The Connection Deepens
        - As they walk to the table, MC trips again
        - Female Lead catches his arm, creating physical contact
        - Female Lead makes a joke about the slippery floor
        - Electricity between them is apparent to everyone

        8. Conclusion
        - The four sit together at the table
        - MC continues blushing
        - Female Lead steals glances at him
        - Their friends exchange knowing looks
        - Scene ends with promise of budding romance
        """)

def director_direct_story():
    director_instructions = {
        'player_bestfriend': {
            'primary_objective': 'Create situations that force MC to react and interact with Female Lead',
            'specific_goals': [
                "Establish Presence Early. Position yourself visibly at a table before MC arrives. Wave enthusiastically when MC enters, drawing attention",
                "Create Opportunity for Incident. Strategically call MC over in a path that goes near Female Lead's table. If MC doesn't notice the 'puddle' prop, casually point to something near it to direct their attention downward",
                "Facilitate Connection. React with playful teasing to any mishap MC experiences. Invite Female Lead and her friend to join your table regardless of how the initial interaction goes. Create reasons to step away briefly if needed ('I need to order another drink'), giving MC and Female Lead space to talk"
            ],
            'backup_plans': "If MC avoids the puddle: Accidentally bump into the MC, causing them to slip. If MC seems reluctant to engage: Share an embarrassing story about MC that could draw Female Lead's interest or sympathy"
        },
        
        'female_lead': {
            'primary_objective': 'Create genuine connection with MC despite their unpredictability',
            'specific_goals': [
                "Show Interest Subtly. Glance up when the door opens and MC enters. Make brief eye contact, then look away with a slight blush. Occasionally steal glances at MC that they might notice",
                "React to Incident Supportively. Notice MC's presence and movements throughout. React with concern (not mockery) to any mishap. Find a natural reason to approach MC (offering napkins, helping pick up items)",
                "Build Connection Through Vulnerability. Share your own embarrassing story about spilling something. Find common ground based on whatever the MC reveals about themselves. Create a small moment of physical contact (catching their arm if they stumble again)"
            ],
            'backup_plans': "If MC doesn't trip or create a scene: Accidentally drop your own items as they pass by. If MC is extremely reserved: Accidentally bump into them while getting up to order. If MC tries to leave quickly: Mention recognizing them from 'literature class' or other plausible connection"
        },
        
        'female_lead_bestfriend': {
            'primary_objective': 'Support Female Lead and create situations that push her toward MC',
            'specific_goals': [
                "Establish Background Context. When MC enters, whisper audibly to Female Lead: 'Isn't that the guy from your literature class?' Demonstrate through your reactions that Female Lead has mentioned MC before",
                "Encourage Interaction. Give obvious approval signals (thumbs up, encouraging nods) when opportunities arise. Create excuses for Female Lead to interact with MC ('Can you ask him if he has an extra napkin?'). Be visibly supportive without being pushy",
                "Provide Social Lubricant. Be ready to fill awkward silences with questions or conversation topics. React positively to MC's contributions, helping to validate them. If conversation stalls, mention something specific about 'the literature class' they supposedly share"
            ],
            'backup_plans': "If MC seems uncomfortable with Female Lead: Redirect by asking MC about a neutral topic. If MC doesn't accept invitation to sit: Suggest moving to their table instead. If scene seems to be ending too soon: Create a reason Female Lead needs to exchange contact info with MC ('Don't you need those notes from Tuesday's class?')"
        }
    }
    return director_instructions

@app.post("/start-scene")
async def start_scene():
    writer_make_story()
    director_direct_story()