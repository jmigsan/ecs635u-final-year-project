from langchain_google_genai import ChatGoogleGenerativeAI
from pydantic import BaseModel, Field

class Person(BaseModel):
    """Information about a person."""

    name: str = Field(..., description="The person's name")
    height_m: float = Field(..., description="The person's height in meters")

llm = ChatGoogleGenerativeAI(model="gemini-2.5-flash-preview-04-17")

messages = [
    (
        "system",
        "You are a helpful assistant that translates English to French. Translate the user sentence.",
    ),
    ("human", "I love programming."),
]
ai_msg = llm.invoke(messages)
print(ai_msg)

structured_llm = llm.with_structured_output(Person)
prompt = "Make up a person"
ai_msg_2 = structured_llm.invoke(prompt)
print(ai_msg_2)