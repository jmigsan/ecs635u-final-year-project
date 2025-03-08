from typing import Annotated, Literal
from typing_extensions import TypedDict
from langgraph.graph import StateGraph
from langgraph.graph.message import add_messages
from langgraph.prebuilt import ToolNode
from langchain_core.tools import tool
from langchain_google_genai import ChatGoogleGenerativeAI
from fastapi import FastAPI
from pydantic import BaseModel
from fastapi import FastAPI, WebSocket, WebSocketDisconnect
import json
import uvicorn

# region FastAPI Websockets
# --------- THIS PART IS ABOUT FASTAPI WEBSOCKETS ---------

app = FastAPI()

class NPCConnectionManager:
    def __init__(self):
        # Each NPC connection will be stored with its ID
        self.active_npcs: dict[str, WebSocket] = {}

    async def connect(self, npc_id: str, websocket: WebSocket):
        await websocket.accept()
        self.active_npcs[npc_id] = websocket
        print(f"NPC {npc_id} connected. Total NPCs: {len(self.active_npcs)}")

    def disconnect(self, npc_id: str):
        del self.active_npcs[npc_id]
        print(f"NPC {npc_id} disconnected. Total NPCs: {len(self.active_npcs)}")

    async def send_to_npc(self, npc_id: str, message: str):
        await self.active_npcs[npc_id].send_text(json.dumps(message))
        return True

manager = NPCConnectionManager()

@app.websocket("/npc/{npc_id}")
async def npc_websocket(websocket: WebSocket, npc_id: str):
    await manager.connect(npc_id, websocket)

    try:
        while True:
            data = await websocket.receive_text()
            # Parse received data
            json_data = json.loads(data)
            print(f"NPC {npc_id} sent: {json_data}")
            
            # Check if this is a message targeted at a specific NPC
            target_npc = json_data.get("target_npc")
            if target_npc:
                # Forward the message to the target NPC
                success = await manager.send_to_npc(target_npc, {
                    "type": "npc_message",
                    "from_npc": npc_id,
                    "message": json_data.get("message", ""),
                    "data": json_data.get("data", {})
                })
                
                # Notify sender of delivery status
                await manager.send_to_npc(npc_id, {
                    "type": "delivery_status",
                    "target_npc": target_npc,
                    "success": success
                })
                
    except WebSocketDisconnect:
        manager.disconnect(npc_id)

# endregion

# region NPC Agent
# ------ THIS PART IS ABOUT THE NPC AGENT -------

class PerceivedState(BaseModel):
    objects_you_can_walk_to: list[str]
    objects_you_can_interact_with: list[str]
    characters_you_can_walk_to: list[str]
    characters_you_can_interact_with: list[str]
    location: str

class State(TypedDict):
    messages: Annotated[list, add_messages]
    perception: PerceivedState

llm = ChatGoogleGenerativeAI(
    model="gemini-2.0-flash-lite",
    google_api_key="AIzaSyBbgaNd1O9xLJK1uai_i8fwGwgMDMRwPjA",
)

@tool
def walk(location: str):
    """Walk to an object in the game world."""
    # tell npc to walk to this part
    # wait for npc controller to respond with what it sees around it
    return

@tool
def interact(object: str):
    """Interact with an object in the game world."""
    # tell npc to interact with object
    # wait for npc controller to respond
    return

@tool
def speak(character: str, message: str):
    """Speak to a character in the game world."""
    # tell npc to speak to character
    # wait for npc controller to respond
    return

tools = [walk, interact, speak]

tool_node = ToolNode(tools)

def prompt_node(state: State) -> State:
    new_message = llm_with_tools.invoke(state["messages"])
    return {"messages": [new_message]}

def perception_node(state: State) -> State: 
    # Placeholder function that would retrieve perception data from Unity
    def get_perception_from_unity() -> PerceivedState:
        return PerceivedState(
            objects_you_can_walk_to=["table", "door", "bookshelf"],
            objects_you_can_interact_with=["book", "lamp", "computer"],
            characters_you_can_walk_to=["Alice", "Bob"],
            characters_you_can_interact_with=["Alice"],
            location="living room",
        )

    new_perception = get_perception_from_unity()
    
    return {
        "messages": state["messages"],
        "perception": new_perception
    }

def conditional_edge(state: State) -> Literal['tool_node', '__end__']:
    last_message = state["messages"][-1]
    if last_message.tool_calls:
        return "tool_node"
    else:
        return "__end__"

llm_with_tools = llm.bind_tools(tools)

graph = StateGraph(State)

graph.add_node("prompt_node", prompt_node)
graph.add_node("tool_node", tool_node)
graph.add_node("perception_node", perception_node)

graph.add_conditional_edges(
    'prompt_node',
    conditional_edge
)

graph.add_edge("tool_node", "perception_node")
graph.add_edge("perception_node", "prompt_node")
graph.set_entry_point("prompt_node")

npc_graph = graph.compile()

def run_agent(message: str):
    # e.g. message = "Walk to the player. Ask them about their day."
    new_state = npc_graph.invoke({"messages": [message]})
    print(new_state)

# endregion

# region __name__
# -------- THIS STARTS UP THE FAST API SERVER ---------

if __name__ == "__main__":
    # replace with code that actually works
    uvicorn.run("npc_server:app", host="0.0.0.0", port=8000, reload=True) 

# endregion