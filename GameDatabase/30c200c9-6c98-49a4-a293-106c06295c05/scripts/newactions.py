
AttackerColor = "#FF0000"
DefenderColor = "#00FF00"
GoldMarker = ("Gold", "4e8046ba-759b-428c-917f-7e9268a5af90")
RenownMarker = ("Power", "d115ea96-ed05-4bf7-ba22-a34c8675c676")
StrengthMarker = ("Strength", "7898d5a0-1d59-42b2-bbfb-5051cc420cd8")
CounterMarker = ("Counter", "6238a357-41b7-4bca-b394-925fc1b2caf8")

firstPlayerToken = "73a6655b-60b6-4080-b428-f4e0099e0f77"

diesides = 6

######################################
##     EVENT FUNCTIONS              ##
######################################


def changeCounterEvent(args):
    mute()
    if args.player == me:
        if args.counter == me.counters["Power"] and args.scripted == False:
            faction = getMyFactionCard()
            if faction != None:
                faction.markers[RenownMarker] = me.Power - (countPower() - faction.markers[RenownMarker])
        elif args.counter == me.counters["Gold"] and args.scripted == False:
            plot = getMyCurrentPlot()
            if plot != None:
                plot.markers[GoldMarker] = me.Gold ## the last plot in the list is the most recently played

def changeMarkerEvent(args):
    mute()
    if args.marker == "Power" and args.card.controller == me:
        power = countPower()
        if me.Power != power:
            me.Power = power
    elif args.marker == "Gold" and args.card.controller == me:
        if args.card == getMyCurrentPlot():
            if me.Gold != args.card.markers[GoldMarker]:
                me.Gold = args.card.markers[GoldMarker]
            

def moveCardEvent(args):
    if args.player == me:
        cattach = eval(getGlobalVariable("cattach"))
        i = 0
        for card in args.cards:
            if args.fromGroups[0] != table and args.toGroups[0] == table:
                if card.isFaceUp:
                    autoAddGold(card)
            else:
                if card.group.name != "Table" or card._id in cattach:
                    attach(card)
                alignAttachments(card, getAttachments(card, cattach))
            i += 1

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
        if card.controller == me and card.isFaceUp == False:
            revealHide(card)

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
        autoAddGold(card)

def autoAddGold(card):
    mute()
    notify("test")
    if card.Income.isdigit():
        if card.markers[GoldMarker] == 0:
            card.markers[GoldMarker] = int(card.Income)

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
    notify("{} added a Gold to {}.".format(me, card))

def addXGold(card, x = 0, y = 0):
    mute()
    num = askInteger("Add how much Gold?", 0)
    if num == 0 or num == None: return
    card.markers[GoldMarker] += num
    notify("{} added {} Gold to {}.".format(me, num, card))

def addRenown(card, x = 0, y = 0):
    mute()
    card.markers[RenownMarker] += 1
    notify("{} added 1 Power to {}.".format(me, card))

def addXRenown(card, x = 0, y = 0):
    mute()
    num = askInteger("Add how much Power?", 0)
    if num == 0 or num == None: return
    card.markers[RenownMarker] += num
    notify("{} added {} Power to {}.".format(me, num, card))

def addStrength(card, x = 0, y = 0):
    mute()
    card.markers[StrengthMarker] += 1
    notify("{} added 1 Strength to {}.".format(me, card))

def addXStrength(card, x = 0, y = 0):
    mute()
    num = askInteger("Add how much Strength?", 0)
    if num == 0 or num == None: return
    card.markers[StrengthMarker] += num
    notify("{} added {} Strength to {}.".format(me, num, card))

def addCounter(card, x = 0, y = 0):
    mute()
    card.markers[CounterMarker] += 1
    notify("{} added 1 counter to {}.".format(me, card))

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

def getAttachments(card, attachdict):
    mute()
    return [k for k,v in attachdict.iteritems() if v == card._id]

def attach(card, x = 0, y = 0):
    mute()
    target = [c for c in table if c.targetedBy]
    if len(target) > 1:
        whisper("Invalid targets, select up to 1 target.")
    else:
        cattach = eval(getGlobalVariable('cattach'))
        if len(target) == 0 or card in target:
            ## DETACH
            card.target(False)
            if card._id in cattach:  ## if this card's an attachment
                notify("{} detaches {} from {}.".format(me, card, Card(cattach[card._id])))
                del cattach[card._id]
                if card.owner != card.controller: ## return card to its owner
                    if card.controller == me:
                        card.controller = card.owner
            for id in getAttachments(card, cattach): ## if the card has attachments
                attachment = Card(id)
                del cattach[id]
                notify("{} detaches {} from {}.".format(me, attachment, card))
                if attachment.owner != attachment.controller:
                    if attachment.controller == me:
                        attachment.controller = attachment.owner
            setGlobalVariable('cattach', str(cattach))
        else:
            ## ATTACH
            targetcard = target[0]
            if targetcard._id in cattach:
                whisper("WARNING: Cannot attach {} to other attachments.".format(targetcard))
                return
            if len(getAttachments(card, cattach)) > 0:  ## Catch cases where you try to attach to another attachment
                whisper("WARNING: Cannot attach {} to other attachments.".format(targetcard))
                return
            cattach[card._id] = targetcard._id
            targetcard.target(False)
            setGlobalVariable('cattach', str(cattach))
            notify("{} attaches {} to {}.".format(me, card, targetcard))
            if targetcard.controller == me:
                alignAttachments(targetcard, getAttachments(targetcard, cattach))
            else:
                card.controller = targetcard.controller
                remoteCall(targetcard.controller, 'alignAttachments', [targetcard, getAttachments(targetcard, cattach)])

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

playerside = None
       
def playerSide():  ## Initializes the player's top/bottom side of table variables
    mute()
    global playerside
    if playerside == None:  ## script skips this if playerside has already been determined
        if Table.isTwoSided():
            if me.isInverted:
                playerside = -1  # inverted (negative) side of the table
            else:
                playerside = 1
        else:  ## If two-sided table is disabled, assume the player is on the normal side.
            playerside = 1
    return playerside

def alignAttachments(card, attachments = None):  ## Aligns all attachments on the card
    mute()
    side = playerSide()
    if attachments == None:
        return
    lastCard = card
    x, y = card.position
    count = 1
    if side*y < 0:  ## A position modifier that has to take into account the vertical orientation of the card
        yyy = -1
    else:
        yyy = 1
    for id in attachments:
        c = Card(id)
        attachY = y + 11 * yyy * side * count ## the equation to identify the y coordinate of the new card
        c.moveToTable(x, attachY)
        c.index = lastCard.index
        lastCard = c
        count += 1

def getMyCurrentPlot():
    mute()
    plots = [c for c in table if c.controller == me and c.Type == "Plot"]
    if len(plots) > 0:
        return plots[-1]
    return None

def getMyFactionCard():
    mute()
    factions = [c for c in table if c.controller == me and c.Type == "Faction"]
    if len(factions) > 0:
        return factions[-1]
    return None

def countPower():
    mute()
    return sum(c.markers[RenownMarker] for c in table if c.controller == me)