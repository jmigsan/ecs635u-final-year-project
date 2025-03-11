# start game

# get perception
# post request from unity. tell server that it's ready--that a game is running. and tells it initial perception.
# do nothing. just log perception. and i guess that the game is running. but mostly just perception.

# when trigger is set, tell server.
# give new perception.
# get writer to make a story using characters and the ONE room (rip soz bozo).
# tell director that they should try to make the charaters follow the story as closely as possible.
# get director to look at the game_state. then tell actors what they should do for the scene. their goals for the scene. director is a work flow. more of a router. it gets called repeatedly by reviewer if things need to change.
# actors get told from python, they have websockets for tools output and perception input only. then they output back into director.
    # tempted to make it so that actors run indefinitely. then they just check their goals to see if it changed or not. each runs on their own thread. director can update goals whenever.
# reviewer gets called EVERY TIME AN ACTOR DOES SOMETHING? given that the player is involved in a story. just to check if the story has been compelted. sure.
# when reviewer decides that act/scene is done, tell director to update npc goals.
# every time player does something, reviewer decides whether npc goals need to change. to tell writer to tell director new story.

# region FastAPI
from fastapi import FastAPI

app = FastAPI()

class StoryManager:
    def __init__(self):
        self.story = ""

story_manager = StoryManager()

@app.post("/start-new-story")
async def start_new_story():
    writer_make_new_story(story_manager)
    director_start_new_story(story_manager)

def writer_make_new_story(story_manager: StoryManager):
    story_manager.story = """
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
        """

    return

def director_start_new_story(story_manager: StoryManager):

    # unique actions: player slips
    # unique npc interactions: bumping into player
    # npc interactions: being close to player, talking to player, calling out to player, waving to player, talking to other npcs, looking at player, dropping items, thumbs up to npc/player, having a group conversation

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

    actor_set_goal('player_bestfriend', director_instructions)
    actor_set_goal('female_lead', director_instructions)
    actor_set_goal('female_lead_bestfriend', director_instructions)

def actor_set_goal(actor: str, director_instructions: dict):
    instructions = director_instructions[actor]
    primary_objective = instructions['primary_objective'] # returns a string
    specific_goals = instructions['specific_goals'] # returns a list
    backup_plans = instructions['backup_plans'] # returns a string



# endregion

# region ReAct Agent

# input: make them fall in love
# create react agent
# if llms stop on their own, v interesting. may not need a reviewer in the way i designed.

# endregion