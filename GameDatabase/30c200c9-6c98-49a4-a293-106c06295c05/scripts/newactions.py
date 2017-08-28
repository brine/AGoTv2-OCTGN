
AttackerColor = "#FF0000"
DefenderColor = "#00FF00"
GoldMarker = ("Gold", "4e8046ba-759b-428c-917f-7e9268a5af90")
RenownMarker = ("Renown", "d115ea96-ed05-4bf7-ba22-a34c8675c676")
CounterMarker = ("Counter", "6238a357-41b7-4bca-b394-925fc1b2caf8")

firstPlayerToken = "73a6655b-60b6-4080-b428-f4e0099e0f77"

diesides = 6

######################################
##     EVENT FUNCTIONS              ##
######################################

def passTurnOverride(args):
    mute()
    if turnNumber() == 0: ## make the turn counter start at 1 on plot phase
        nextTurn()
        setPhase(1)
        return
    firstPlayer = getFirstPlayer()
    if firstPlayer == None: ## clicking a turn button with no first player will assign it to that player
        setFirstPlayer(args.player)
    elif getActivePlayer() == None: ## when there's no active player but first player was already assigned
        setActivePlayer(firstPlayer)
    else:
        nextPlayer = getNextPlayer(getActivePlayer())
        if nextPlayer == firstPlayer:  ## when the turn is being passed to the first player, it should go to the next phase
            phaseName, phaseId = currentPhase()
            if phaseId == 7: ## passing at end of turn
                setFirstPlayer(None)
                nextTurn()
                setPhase(1)
            else:
                setActivePlayer(nextPlayer)
                setPhase(phaseId + 1)
        else:
            setActivePlayer(nextPlayer)
            setPhase(currentPhase()[1]) #do this just to announce the new active player in the chat

def phaseClickOverride(args):
    mute()
    if getActivePlayer() == me: ## only the active player can change phases
        setPhase(args.id)
        if getFirstPlayer() == None:
            setFirstPlayer(me)

def getFirstPlayer():
    mute()
    firstPlayer = getGlobalVariable("firstplayer")
    if firstPlayer == "None":
        return None
    try:
        firstPlayer = Player(int(firstPlayer))
        return firstPlayer
    except:
        setFirstPlayer(None)
        return None

def getNextPlayer(currentPlayer):
    mute()
    playerList = sorted([x._id for x in getPlayers()])
    nextPlayers = [x for x in playerList if x > currentPlayer._id]
    if len(nextPlayers) == 0: ##if we're at the end of the players list
        nextPlayerId = playerList[0]
    else:
        nextPlayerId = nextPlayers[0]
    return Player(nextPlayerId)

def setFirstPlayer(player):
    mute()
    setActivePlayer(player)
    if player == None: ## removes the first player
        setGlobalVariable("firstplayer", "None")
    else:
        notify("{} becomes the First Player".format(player))
        setGlobalVariable("firstplayer", str(player._id))

def initializeGame():
    mute()
    #### LOAD UPDATES
    v1, v2, v3, v4 = gameVersion.split('.')  ## split apart the game's version number
    v1 = int(v1) * 1000000
    v2 = int(v2) * 10000
    v3 = int(v3) * 100
    v4 = int(v4)
    currentVersion = v1 + v2 + v3 + v4  ## An integer interpretation of the version number, for comparisons later
    lastVersion = getSetting("lastVersion", convertToString(currentVersion - 1))  ## -1 is for players experiencing the system for the first time
    lastVersion = int(lastVersion)
    for log in sorted(changelog):  ## Sort the dictionary numerically
        if lastVersion < log:  ## Trigger a changelog for each update they haven't seen yet.
            stringVersion, date, text = changelog[log]
            updates = '\n-'.join(text)
            confirm("What's new in {} ({}):\n-{}".format(stringVersion, date, updates))
    setSetting("lastVersion", convertToString(currentVersion))  ## Store's the current version to a setting

######################################
##           TABLE ACTIONS          ##
######################################

def respond(group, x = 0, y = 0):
    notify('{} RESPONDS!'.format(me))

def passPriority(group, x = 0, y = 0):
    passTurnOverride(EventArgument({"player": me}))

def becomeFirstPlayer(group, x = 0, y = 0):
    mute()
    setFirstPlayer(me)

def initiateMilitary(group, x = 0, y = 0):
    mute()
    for card in table:
        if card.controller == me:
            card.highlight = None
    notify("{} initiates a Military challenge".format(me))
    setGlobalVariable("challenge", "mil")

def initiateIntrigue(group, x = 0, y = 0):
    mute()
    for card in table:
        if card.controller == me:
            card.highlight = None
    notify("{} initiates an Intrigue challenge".format(me))
    setGlobalVariable("challenge", "int")

def initiatePower(group, x = 0, y = 0):
    mute()
    for card in table:
        if card.controller == me:
            card.highlight = None
    notify("{} initiates a Power challenge".format(me))
    setGlobalVariable("challenge", "pow")

def activateAbility(card, x = 0, y = 0):
    notify("{} activates {}'s ability.".format(me, card))

def clearChallenges(group, x = 0, y = 0):
    mute()
    for card in table:
        if card.controller == me:
            card.highlight = None
            card.target(False)
    notify("{} clears all targets and challenge indicators.".format(me))

def standAll(group, x = 0, y = 0):
    mute()
    for card in table:
        if card.controller == me:
            if card.orientation != Rot0:
                card.orientation = Rot0
    notify("{} stands their cards.".format(me))

def revealAll(group, x = 0, y = 0):
    mute()
    for card in table:
        if card.controller == me:
            if card.isFaceUp == False:
                card.isFaceUp = True
                notify("{} reveals {}.".format(me, card))


def setDie(group, x = 0, y = 0):
    mute()
    global diesides
    num = askInteger("How many sides?\n\nFor Coin, enter 2", diesides)
    if num != None and num > 0:
        diesides = num
        dieFunct(diesides)

def rollDie(group, x = 0, y = 0):
    mute()
    global diesides
    dieFunct(diesides)

def dieFunct(num):
    if num == 2:
        n = rnd(1, 2)
        if n == 1:
            notify("{} rolls 1 (HEADS) on a 2-sided die.".format(me))
        else:
            notify("{} rolls 2 (TAILS) on a 2-sided die.".format(me))
    else:
        n = rnd(1, num)
        notify("{} rolls {} on a {}-sided die.".format(me, n, num))

def createTitles(group, x = 0, y = 0):
    mute()
    tableCards = [card.model for card in table]
    for title in queryCard({"Type": "Title"}):
        if title not in tableCards:
            titleCard = table.create(title, x, y, 1)
            x += 10
            if titleCard.isInverted():
                y -= 10
            else:
                y += 10
    notify("{} loaded the Title cards.".format(me))
        

######################################
##            CARD ACTIONS          ##
######################################
def getChallengeColor(block = False):
    mute()
    challenge = getGlobalVariable("challenge")
    if challenge == "mil":
        if block == True:
            return "#880000"
        else:
            return "#FF0000"
    elif challenge == "int":
        if block == True:
            return "#008800"
        else:
            return "#00FF00"
    elif challenge == "pow":
        if block == True:
            return "#000088"
        else:
            return "#0000FF"
    else:
        return "#FFFFFF"

def kneelStand(card, x = 0, y = 0):
    mute()
    if card.orientation == Rot0:
        card.orientation = Rot90
        notify("{} kneels {}.".format(me, card))
    else:
        card.orientation = Rot0
        notify("{} stands {}.".format(me, card))

def revealHide(card, x = 0, y = 0):
    mute()
    if card.isFaceUp:
        card.isFaceUp = False
        notify("{} hides {}.".format(me, card))
    else:
        card.isFaceUp = True
        notify("{} reveals {}.".format(me, card))

def assignAttacker(card, x = 0, y = 0):
    mute()
    if card.orientation == Rot90:
        if not confirm("Can't declare a knelt character as an attacker. Ignore?"): return
    card.orientation = Rot90
    card.highlight = getChallengeColor()
    notify("{} declares {} as an attacker.".format(me, card))

def assignAttackerNoKneel(card, x = 0, y = 0):
    mute()
    if card.orientation == Rot90:
        if not confirm("Can't declare a knelt character as an attacker. Ignore?"): return
    card.highlight = getChallengeColor()
    notify("{} declares {} as an attacker.".format(me, card))
    
def assignDefender(card, x = 0, y = 0):
    mute()
    if card.orientation == Rot90:
        if not confirm("Can't declare a knelt character as a defender. Ignore?"): return
    card.orientation = Rot90
    card.highlight = getChallengeColor(True)
    notify("{} declares {} as a defender.".format(me, card))

def assignDefenderNoKneel(card, x = 0, y = 0):
    mute()
    if card.orientation == Rot90:
        if not confirm("Can't declare a knelt character as a defender. Ignore?"): return
    card.highlight = getChallengeColor(True)
    notify("{} declares {} as a defender.".format(me, card))

def addGold(card, x = 0, y = 0):
    mute()
    card.markers[GoldMarker] += 1
    notify("{} added a gold marker to {}.".format(me, card))

def addXGold(card, x = 0, y = 0):
    mute()
    num = askInteger("Add how many gold markers?", 0)
    if num == 0 or num == None: return
    card.markers[GoldMarker] += num
    notify("{} added {} gold marker{} to {}.".format(me, num, pluralize(num), card))

def addRenown(card, x = 0, y = 0):
    mute()
    card.markers[RenownMarker] += 1
    notify("{} added a renown marker to {}.".format(me, card))

def addXRenown(card, x = 0, y = 0):
    mute()
    num = askInteger("Add how many renown markers?", 0)
    if num == 0 or num == None: return
    card.markers[RenownMarker] += num
    notify("{} added {} renown marker{} to {}.".format(me, num, pluralize(num), card))

def addCounter(card, x = 0, y = 0):
    mute()
    card.markers[CounterMarker] += 1
    notify("{} added a counter to {}.".format(me, card))

def addXCounter(card, x = 0, y = 0):
    mute()
    num = askInteger("Add how many counters?", 0)
    if num == 0 or num == None: return
    card.markers[CounterMarker] += num
    notify("{} added {} counter{} to {}.".format(me, num, pluralize(num), card))

def killCard(card, x = 0, y = 0):
    mute()
    if card.Type != "Character":
        discardCard(card)
    else:
        card.moveTo(card.owner.piles["Dead Pile"])
        notify("{}'s {} is killed.".format(me, card))

def discardCard(card, x = 0, y = 0):
    mute()
    card.moveTo(card.owner.piles["Discard Pile"])
    notify("{} discards {}.".format(me, card))

def clear(card, x = 0, y = 0):
    card.highlight = None
    card.target(False)
    notify("{} clears {}.".format(me, card))

######################################
##            PILE ACTIONS          ##
######################################

def draw(group, x = 0, y = 0):
    mute()
    if len(group) == 0:
        return
    card = group[0]
    card.moveTo(card.owner.hand)
    notify("{} draws a card.".format(me))

def drawMany(group, x = 0, y = 0):
    mute()
    if len(group) == 0:
        return
    count = askInteger("Draw how many cards?", 7)
    if count == None or count == 0:
        return
    for card in group.top(count):
        card.moveTo(card.owner.hand)
    notify("{} draws {} card{}.".format(me, count, pluralize(count)))

def shuffle(group, x = 0, y = 0, silence = False):
    mute()
    for card in group:
        if card.isFaceUp:
            card.isFaceUp = False
    group.shuffle()
    if silence == False:
        notify("{} shuffled their {}".format(me, group.name))

def mulligan(group, x = 0, y = 0):
    mute()
    newCount = len(group)
    if newCount < 0:
        return
    if not confirm("Confirm Mulligan?"):
        return
    notify("{} mulligans.".format(me))
    for card in group:
        card.moveTo(card.owner.Deck)
    shuffle(me.Deck, silence = True)
    for card in me.Deck.top(newCount):
        card.moveTo(card.owner.hand)
        
def randomDiscard(group, x = 0, y = 0):
    mute()
    card = group.random()
    if card == None:
        return
    card.moveTo(card.owner.piles["Discard Pile"])
    notify("{} randomly discards {} from {}.".format(me, card, group.name))

def pillage(group, x = 0, y = 0):
    mute()
    if len(group) == 0: return
    group[0].moveTo(card.owner.piles["Discard Pile"])
    notify("{} pillages {}.".format(me, card))

def moveToPlot(group, x = 0, y = 0):
    mute()
    if len(group) == 0: return
    for card in group:
        card.moveTo(card.owner.piles["Plot Deck"])
    notify("{} moved all used plots to their Plot Deck.".format(me))

def viewGroup(group, x = 0, y = 0):
    group.lookAt(-1)


######################################
##    HELPER FUNCTIONS              ##
######################################

def pluralize(num):
   if num == 1:
       return ""
   else:
       return "s"
