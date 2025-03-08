import socketio
import asyncio
import uuid
from typing import Dict, Any, List, Annotated, Literal
from typing_extensions import TypedDict
from pydantic import BaseModel
from langchain_core.messages import HumanMessage
from langchain_google_genai import ChatGoogleGenerativeAI
from langchain_core.tools import tool
from langgraph.graph import StateGraph
from langgraph.graph.message import add_messages
from langgraph.prebuilt import ToolNode

# Socket.IO setup
sio = socketio.AsyncClient()
is_connected = False

class PerceivedState(BaseModel):
    objects_you_can_walk_to: list[str]
    objects_you_can_interact_with: list[str]
    characters_you_can_walk_to: list[str]
    characters_you_can_interact_with: list[str]
    location: str
    npc_id: str

class State(TypedDict):
    messages: Annotated[list, add_messages]

class NPC:
    def __init__(self, name: str):
        self.name = name
        self.perception = None
        self.response_event = asyncio.Event()
        self.running = False
        self.graph = None
        self.agent = None

        self.llm = ChatGoogleGenerativeAI(
            model="gemini-2.0-flash-lite",
            google_api_key="AIzaSyBbgaNd1O9xLJK1uai_i8fwGwgMDMRwPjA",
        )

        self.create_agent()

    @tool
    async def walk(self, location: str) -> str:
        """Walk to an object or location in the game world."""
        self.response_event.clear()
        await sio.emit('npc_command', {
            'action': 'walk',
            'target': location,
            'npc_name': self.name
        })
        await self.response_event.wait()
        return self.perception
    
    @tool
    async def interact(self, object: str) -> str:
        """Interact with an object in the game world."""
        self.response_event.clear()
        await sio.emit('npc_command', {
            'action': 'interact',
            'target': object,
            'npc_name': self.name
        })
        await self.response_event.wait()
        return self.perception
    
    @tool
    async def speak(self, character: str, message: str) -> str:
        """Speak to a character in the game world."""
        self.response_event.clear()
        await sio.emit('npc_command', {
            'action': 'speak',
            'target': character,
            'message': message,
            'npc_name': self.name
        })
        await self.response_event.wait()
        return self.perception
    
    def setup_graph(self):
        # Create the state graph
        graph = StateGraph(State)
        
        tools = [self.walk, self.interact, self.speak]
        
        # Bind tools to the LLM
        prompt = f"""You are {self.name}, an NPC in a virtual world. {self.personality}
        
            You can perceive the world around you and take actions based on what you see.
            You can walk to objects and characters, interact with objects, and speak to characters.

            Your current perception will be provided to you.
            Think about what you want to do based on your personality and current situation.
            Use the tools available to you to take actions in the world.

            Always act in character and according to your personality.
            """
        
        llm_with_tools = self.llm.bind_tools(tools)
        tool_node = ToolNode(tools)
        
        graph.add_node("tool_node", tool_node)
        
        def prompt_node(state: State) -> State:
            new_message = llm_with_tools.invoke(state["messages"])
            return {"messages": [new_message]}
        
        graph.add_node("prompt_node", prompt_node)
        
        def conditional_edge(state: State) -> Literal['tool_node', '__end__']:
            last_message = state["messages"][-1]
            if last_message.tool_calls:
                return "tool_node"
            else:
                return "__end__"
        
        graph.add_conditional_edges(
            'prompt_node',
            conditional_edge
        )
        graph.add_edge("tool_node", "prompt_node")
        graph.set_entry_point("prompt_node")
        
        self.graph = graph
        self.agent = graph.compile()

npcs = {}

# Connect to Socket.IO server
@sio.event
async def connect():
    global is_connected
    print("Connected to Unity server")
    is_connected = True

@sio.event
async def disconnect():
    global is_connected
    print("Disconnected from Unity server")
    is_connected = False

@sio.event
async def perception_update(data):
    """Handler for receiving perception updates from Unity"""
    # Extract NPC ID from the data
    npc_id = data.get("npc_id")
    if npc_id in npcs:
        # Update the perception for this specific NPC
        npcs[npc_id].perception = PerceivedState(**data)
        # Signal that we've received a response for this NPC
        npcs[npc_id].response_event.set()
        print(f"Received perception update for NPC {npcs[npc_id].name}")

# Create a new NPC
async def create_npc(npc_id: str, name: str, personality: str):
    # Create NPC object
    npc = NPC(npc_id, name, personality)
    
    # Store in our dictionary
    npcs[npc_id] = npc
    
    return npc

async def main():
    # Connect to Unity
    await sio.connect('http://localhost:3000')
    
    # Create NPCs
    farmer = await create_npc(
        "villager1", 
        "Village Farmer", 
        "You are a hardworking farmer who loves to talk about crops and weather."
    )
    
    guard = await create_npc(
        "guard1", 
        "Town Guard", 
        "You are a vigilant guard who takes your duty seriously. You're suspicious of strangers."
    )
    
    # Start NPC behaviors
    farmer_task = asyncio.create_task(farmer.start_behavior(10.0))
    guard_task = asyncio.create_task(guard.start_behavior(12.0))
    
    # Run for a while
    try:
        await asyncio.sleep(300)  # Run for 5 minutes
    finally:
        # Stop NPCs
        farmer.stop_behavior()
        guard.stop_behavior()
        
        # Wait for tasks to complete
        await farmer_task
        await guard_task
        
        # Disconnect
        await sio.disconnect()

if __name__ == "__main__":
    asyncio.run(main())