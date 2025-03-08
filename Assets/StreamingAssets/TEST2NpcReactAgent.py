# Python side - Individual NPC Management
import socketio
import asyncio
import uuid
from typing import Dict, Any, List
from pydantic import BaseModel
from langchain_core.messages import HumanMessage
from langchain_google_genai import ChatGoogleGenerativeAI
from langchain.agents import AgentExecutor, create_react_agent

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

class NPC:
    def __init__(self, npc_id: str, name: str, personality: str):
        self.id = npc_id
        self.name = name
        self.personality = personality
        self.perception = None
        self.response_event = asyncio.Event()
        self.running = False
        self.agent_executor = None
        
        # Initialize LLM
        self.llm = ChatGoogleGenerativeAI(
            model="gemini-2.0-flash-lite",
            google_api_key="AIzaSyBbgaNd1O9xLJK1uai_i8fwGwgMDMRwPjA",
        )
        
        # Create agent with tools defined as methods
        self.create_agent()
    
    @tool
    async def walk(self, location: str) -> str:
        """Walk to an object or location in the game world."""
        self.response_event.clear()
        await sio.emit('npc_command', {
            'action': 'walk',
            'target': location,
            'npc_id': self.id
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
            'npc_id': self.id
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
            'npc_id': self.id
        })
        await self.response_event.wait()
        return self.perception
    
    def create_agent(self):
        prompt = f"""You are {self.name}, an NPC in a virtual world. {self.personality}
        
You can perceive the world around you and take actions based on what you see.
You can walk to objects and characters, interact with objects, and speak to characters.

Your current perception will be provided to you.
Think about what you want to do based on your personality and current situation.
Use the tools available to you to take actions in the world.

Always act in character and according to your personality.
"""
        
        agent = create_react_agent(self.llm, self.tools, prompt)
        self.agent_executor = AgentExecutor(agent=agent, tools=self.tools, verbose=True)
    
    async def run_step(self, goal: str = None):
        if not self.perception:
            return f"NPC {self.name} has no perception data yet"
        
        # Construct the input with current perception
        input_text = f"""
Current perception:
- Location: {self.perception.location}
- Objects you can walk to: {', '.join(self.perception.objects_you_can_walk_to)}
- Objects you can interact with: {', '.join(self.perception.objects_you_can_interact_with)}
- Characters you can walk to: {', '.join(self.perception.characters_you_can_walk_to)}
- Characters you can interact with: {', '.join(self.perception.characters_you_can_interact_with)}
"""
        
        if goal:
            input_text += f"\nYour current goal: {goal}"
        
        # Run the agent
        result = await self.agent_executor.ainvoke({"input": input_text})
        
        return result["output"]
    
    async def start_behavior(self, interval: float = 5.0):
        self.running = True
        
        # Request initial perception
        await sio.emit('request_perception', {'npc_id': self.id})
        
        # Wait for initial perception
        await self.response_event.wait()
        
        while self.running:
            try:
                result = await self.run_step()
                print(f"NPC {self.name}: {result}")
                
                # Wait before next action
                await asyncio.sleep(interval)
            except Exception as e:
                print(f"Error in NPC {self.name} behavior loop: {e}")
                await asyncio.sleep(interval)
    
    def stop_behavior(self):
        self.running = False
        return f"Stopped NPC {self.name}"

# Dictionary to store all NPCs
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

# Main function to start the system
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