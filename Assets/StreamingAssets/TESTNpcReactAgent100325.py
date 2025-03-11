# region LangChain Stuff

from typing import Annotated, Literal
from typing_extensions import TypedDict
from langgraph.graph import StateGraph
from langgraph.graph.message import add_messages
from langgraph.prebuilt import ToolNode, InjectedState
from langchain_core.tools import tool
from langchain_google_genai import ChatGoogleGenerativeAI

active_connections = {}

class State(TypedDict):
    messages: Annotated[list, add_messages]
    websocket_id: str

llm = ChatGoogleGenerativeAI(
    model="gemini-2.0-flash-lite",
    google_api_key="AIzaSyBbgaNd1O9xLJK1uai_i8fwGwgMDMRwPjA",
)

@tool
def get_weather(location: str, state: Annotated[dict, InjectedState]) -> str:
    """Call to get the current weather."""

    print(f"wss {state['websocket_id']}")

    if location.lower() in ["yorkshire"]:
        return "It's cold and wet."
    elif location.lower() in ["london"]:
        return "It's windy and cloudy."
    else:
        return "It's warm and sunny."
    
def prompt_node(state: State) -> State:
    new_message = llm_with_tools.invoke(state["messages"])
    return {
        "messages": [new_message],
        "websocket_id": state.get("websocket_id")
        }

def conditional_edge(state: State) -> Literal['tool_node', '__end__']:
    last_message = state["messages"][-1]
    if last_message.tool_calls:
        return "tool_node"
    else:
        return "__end__"

tools = [get_weather]

graph = StateGraph(State)

llm_with_tools = llm.bind_tools(tools)

tool_node = ToolNode(tools)

graph.add_node("tool_node", tool_node)
graph.add_node("prompt_node", prompt_node)

graph.add_conditional_edges(
    'prompt_node',
    conditional_edge
)

graph.add_edge("tool_node", "prompt_node")
graph.set_entry_point("prompt_node")

# endregion

# region FastAPI Stuff
from fastapi import FastAPI, WebSocket, WebSocketDisconnect
import uuid

app = FastAPI()

@app.websocket("/ws")
async def websocket_endpoint(websocket: WebSocket):
    await websocket.accept()

    websocket_id = str(uuid.uuid4())
    active_connections[websocket_id] = websocket

    try:
        while True:
            data = await websocket.receive_json()

            agent = graph.compile()
            agent_response = agent.invoke({
                "messages": [f"What's the weather in {data['location']}?"], 
                "websocket_id": websocket_id
                })

            stripped_agent_response = agent_response["messages"][-1].content

            await websocket.send_text(f"agent response: {stripped_agent_response}")

    except WebSocketDisconnect:
        print("Client disconnected")

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main3:app", host="127.0.0.1", port=8000, reload=True)

# endregion