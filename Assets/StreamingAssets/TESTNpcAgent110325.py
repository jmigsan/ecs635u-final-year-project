from fastapi import FastAPI, WebSocket, WebSocketDisconnect
import json

app = FastAPI()

active_connections: dict[str, WebSocket] = {}
pending_responses: dict[str, dict[str, asyncio.Future]] = {}

@app.websocket("/ws/{client_id}")
async def websocket_endpoint(websocket: WebSocket, client_id: str):
    await websocket.accept()
    active_connections[client_id] = websocket

    try:
        while True:
            # Receive message from client
            message_text = await websocket.receive_text()
            try:
                # Parse the message as JSON
                message = json.loads(message_text)
                
                # Check if this is a response to a pending action
                if "action_id" in message:
                    action_id = message["action_id"]
                    if (client_id in pending_responses and 
                        action_id in pending_responses[client_id]):
                        # Resolve the waiting future with the response data
                        pending_responses[client_id][action_id].set_result(message)
            except json.JSONDecodeError:
                print(f"Received non-JSON message: {message_text}")

    except WebSocketDisconnect:
        print("Client disconnected")
        if client_id in active_connections:
            del active_connections[client_id]
        
        # Clean up any pending responses for this client
        if client_id in pending_responses:
            for future in pending_responses[client_id].values():
                if not future.done():
                    future.set_exception(ConnectionError("WebSocket disconnected"))
            del pending_responses[client_id]


### ------------------
### ------------------

from langchain_google_genai import ChatGoogleGenerativeAI
from langgraph.checkpoint.memory import MemorySaver
from langchain_core.tools import tool
from langgraph.prebuilt import create_react_agent
from langgraph.prebuilt.chat_agent_executor import AgentState
from langgraph.prebuilt import InjectedState
from typing_extensions import Annotated
import uuid
import asyncio
import textwrap

model = ChatGoogleGenerativeAI(
    model="gemini-2.0-flash-lite",
    google_api_key="AIzaSyBbgaNd1O9xLJK1uai_i8fwGwgMDMRwPjA",
)

memory = MemorySaver()

goal = "Have a nice talk with Gerard."

class State(AgentState):
    client_id: str

async def wait_for_action_completion(client_id: str, action_id: str, timeout: int = 120):
    """Wait for a response from the client for a specific action."""
    # Create a future to wait on
    if client_id not in pending_responses:
        pending_responses[client_id] = {}
    
    future = asyncio.Future()
    pending_responses[client_id][action_id] = future
    
    try:
        # Wait for the response with timeout
        return await asyncio.wait_for(future, timeout=timeout)
    finally:
        # Clean up
        if client_id in pending_responses and action_id in pending_responses[client_id]:
            del pending_responses[client_id][action_id]
            if not pending_responses[client_id]:
                del pending_responses[client_id]

@tool
async def walk(target: str, state: Annotated[dict, InjectedState]):
    """Walk to any target."""
    
    if state["client_id"] in active_connections:
        websocket = active_connections[state["client_id"]]
        
        action_id = str(uuid.uuid4())
        
        await websocket.send_json({
            "action": "walk",
            "action_id": action_id,
            "args": target
        })
        
        try:
            response = await wait_for_action_completion(state["client_id"], action_id)
            return_prompt = textwrap.dedent(f"""
                Successfully walked to {target}
                State: {response.get('new_state')}
                Goal: {goal}
                """)

            return return_prompt
        except asyncio.TimeoutError:
            return f"Attempted to walk to {target}, but didn't receive confirmation within the timeout period."
        
    else:
        print("rip bozo: walking")

@tool
async def talk(target: str, message: str, state: Annotated[dict, InjectedState]):
    """Talk to any target and tell them a message."""

    if state["client_id"] in active_connections:
        websocket = active_connections[state["client_id"]]
        
        action_id = str(uuid.uuid4)

        await websocket.send_json({
            "action": "talk",
            "action_id": action_id,
            "args": {
                "target": target,
                "message": message
            }
        })

        try:
            response = await wait_for_action_completion(state["client_id"], action_id)
            return_prompt = textwrap.dedent(f"""
                Successfully talked to {target}
                State: {response.get('new_state')}
                Goal: {goal}
                """)

            return return_prompt
        except asyncio.TimeoutError:
            return f"Attempted to talk to {target}, but didn't receive confirmation within the timeout period."
    else:
        print("rip bozo: talking")

tools = [walk, talk]

config = {"configurable": {"thread_id": "1"}}
system_prompt = "You are an actor in a play. Follow the directions given to you. You can only interact with things less than 1 meter away from you. Interactions include: talking."

graph = create_react_agent(model, tools=tools, config=config, checkpointer=memory, prompt=system_prompt)

def print_stream(stream):
    for s in stream:
        message = s["messages"][-1]
        if isinstance(message, tuple):
            print(message)
        else:
            message.pretty_print()

input_prompt = f"""
State: You are at the park. You are next to a tree. Felix is playing with a kite 10 meters east of you. Diana is playing with her child 10 meters south of you. Gerard is sitting by a tree 10 meters north of you. 
Goal: {goal}
"""

inputs = {"messages": [("user", input_prompt)]}
print_stream(graph.stream(inputs, stream_mode="values"))