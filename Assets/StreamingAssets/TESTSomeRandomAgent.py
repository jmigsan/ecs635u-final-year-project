# Install required packages:
# pip install langchain langgraph langchain-openai

import os
from typing import Annotated, List, TypedDict
from langchain_core.messages import AnyMessage, HumanMessage, AIMessage, ToolMessage
from langchain_core.tools import tool
from langgraph.graph import StateGraph, END
from langgraph.graph.message import add_messages
from langchain_google_genai import ChatGoogleGenerativeAI

# Define the state structure
class State(TypedDict):
    messages: Annotated[List[AnyMessage], add_messages]

# Initialize the language model
llm = ChatGoogleGenerativeAI(
    model="gemini-2.0-flash",
    google_api_key="AIzaSyBbgaNd1O9xLJK1uai_i8fwGwgMDMRwPjA",
)

# Define a mock search tool
@tool
def search(query: str) -> str:
    """Search for information based on the query."""
    return f"Search result for '{query}': This is a mock response providing information about {query}."

# Bind tools to the language model
tools = [search]
llm_with_tools = llm.bind_tools(tools)

# Define the agent node
def agent(state: State) -> State:
    response = llm_with_tools.invoke(state["messages"])
    return {"messages": [response]}

# Define the tools node
def tool_node(state: State) -> State:
    last_message = state["messages"][-1]
    tool_calls = last_message.tool_calls
    tool_results = []

    for tool_call in tool_calls:
        tool_name = tool_call["name"]
        tool_args = tool_call["args"]
        if tool_name == "search":
            result = search(tool_args["query"])
            tool_results.append(
                ToolMessage(content=result, tool_call_id=tool_call["id"])
            )
    
    return {"messages": tool_results}

# Construct the graph
graph = StateGraph(State)

# Add nodes
graph.add_node("agent", agent)
graph.add_node("tools", tool_node)

# Set the entry point
graph.set_entry_point("agent")

# Define conditional routing
def should_continue(state: State):
    last_message = state["messages"][-1]
    if last_message.tool_calls:
        return "tools"
    return END

# Add edges
graph.add_conditional_edges("agent", should_continue)
graph.add_edge("tools", "agent")

# Compile the graph
compiled_graph = graph.compile()

# Function to run the agent and print the final answer
def run_agent(query: str):
    initial_state = {"messages": [HumanMessage(content=query)]}
    final_state = compiled_graph.invoke(initial_state)
    
    # Print all messages for debugging
    print("Conversation history:")
    for msg in final_state["messages"]:
        if isinstance(msg, HumanMessage):
            print(f"Human: {msg.content}")
        elif isinstance(msg, AIMessage) and not msg.tool_calls:
            print(f"Agent: {msg.content}")
        elif isinstance(msg, ToolMessage):
            print(f"Tool: {msg.content}")
        elif isinstance(msg, AIMessage) and msg.tool_calls:
            print(f"Agent (tool call): {msg.tool_calls}")
    
    # Extract and return the final answer
    for msg in reversed(final_state["messages"]):
        if isinstance(msg, AIMessage) and not msg.tool_calls:
            return f"Final Answer: {msg.content}"
    return "No final answer provided."

# Example usage
if __name__ == "__main__":
    query = "What is Python programming?"
    result = run_agent(query)
    print("\n" + result)