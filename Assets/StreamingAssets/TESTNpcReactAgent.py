from typing import Annotated, Literal
from typing_extensions import TypedDict
from langgraph.graph import StateGraph
from langgraph.graph.message import add_messages
from langgraph.prebuilt import ToolNode
from langchain_core.tools import tool
from langchain_google_genai import ChatGoogleGenerativeAI

class State(TypedDict):
    messages: Annotated[list, add_messages]

@tool
def walk(location: str):
    """Walk to a location in the world."""
    return f"New game state. You walked to {location}"

@tool
def talk(words: str, entity: str):
    """Say something to someone. Tell an NPC or the player something."""
    return f"New game state. You said '{words}' to {entity}"

llm = ChatGoogleGenerativeAI(
    model="gemini-2.0-flash",
    google_api_key="AIzaSyBbgaNd1O9xLJK1uai_i8fwGwgMDMRwPjA",
)

tools = [walk, talk]
llm_with_tools = llm.bind_tools(tools)

tool_node = ToolNode(tools)

def prompt_node(state: State):
    new_message = llm_with_tools.invoke(state["messages"])
    return {"messages": [new_message]}

def conditional_edge(state: State) -> Literal['tool_node', '__end__']:
    last_message = state["messages"][-1]
    if last_message.tool_calls:
        return "tool_node"
    else:
        return "__end__"