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

graph = StateGraph(State)
graph.add_node("tool_node", tool_node)
graph.add_node("prompt_node", prompt_node)
graph.add_conditional_edges(
    'prompt_node',
    conditional_edge
)
graph.add_edge("tool_node", "prompt_node")
graph.set_entry_point("prompt_node")

compiled_graph = graph.compile()

initial_state = {"messages": ["Walk up to the player. Initiate a conversation about your favorite authors."]}
final_state = compiled_graph.invoke(initial_state)

print(final_state["messages"][-1].content)