# Versus

Versus is a Cats vs Dogs strategy game combining city-wide top down strategy with on the ground FPS tactics. The game is an entry into the [7DFPS game jam, 2021](http://7dayfps.com/).


# Playing

In Versus it’s Cats Vs Dogs in an endless battle for territory. Two modes see you play strategically in a top down city as a cat general. Or tactically as in an FPS as a cat lover.

Each animal lives in a block. If there are enough of one species living in a block they will have dominance over that block. The animals will lay repellent mines and other weapons in a quest to drive out other animals.

As the cat general you can command your forces to focus on a particular block in an attempt to take control by driving out the dogs. You can also take control of a human who isarmed with dog repellent weaponry. In human form you operate in first person mode.

When animals are hit by repellent they will run away. If they are hit by enough they will leave the block and head to a friendly block where they will spend some time recovering before going back into battle.

If a fleeing animal cannot find a nearby friendly block they will leave the city entirely.

## 7DFPS Game Jam - Rules

The rules of this jam are simple:

Question: Can I...?
Answer: Yes!

# Running

At the time of writing we are in development and have not cut any releases. You will therefore need Unity 2021.2.0f1 and a checkout of this repository.

Once in Unity you can find all our project assets in `Assets/_Versus`.

A good place to start is `Assets/_Versus/Scenes/` which contains the main scenes of the game. Start with `Main`.

Another interesting place is `Assets/_Versus/Scenes/Experiments`, this is where we try stuff out before merging changes into the main prefabs and scenes.


# Contributing

This project is open source. It depends on Neo FPS but other than that there are no paid assets used.

## Working with Git

This is not a detailed tutorial, just an indicator of how we work:

1. Fork the project on GitHub
2. Checkout your fork and set it up for collaboration with the following commands:
```
git remote add upstream git@github.com:TheWizardsCode/Versus.git
```
3. Do your work using the Git Flow process (TL;DR `git checkout -b MyFeatureBranch`)
4. Issue a PR on GitHub from your feature branch
5. Work with the maintainers to have the PR merged (we may ask for changes to bring it inline with our goals)
6. When merged run the following commands:
```
git fetch upstream
git rebase upstream/main
```