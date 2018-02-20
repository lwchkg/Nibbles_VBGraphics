# Nibbles for VB.NET

This is a remake of [QBasic Nibbles](https://en.wikipedia.org/wiki/Nibbles_(video_game)) in VB.NET with the [VBGraphics](https://www.nuget.org/packages/VBGraphics/) package.
QBasic Nibbles was published with MS-DOS version 5.0 or above, and served as an example of what QBasic can do.

Like [Nibbles for VB.NET](https://github.com/lwchkg/nibbles_vbnet), the VBGraphics version is intended to be an example program for beginners in their first year of programming.
It also demonstrate the capabilities of the VBGraphics package.

![Title Screen](screenshot/title_screen.png?raw=true)
![Gameplay](screenshot/gameplay.png?raw=true)

## Playing the game

If you just want to try the game, go to [the latest release](https://github.com/lwchkg/Nibbles_VBGraphics/releases), download the binary and just run it.
Then follow the in-game instructions.

**Note: the “Close” button in the game window currently cannot be used to end the game.
So the only way to close the program is to type “N” in the game over screen.
This will be fixed in a future version.**

But as an example program, it is much more useful to download the source instead by the following instructions:

1. Install Visual Studio if you did not already install it.
   The Community Version of Visual Studio is free to use for open-source projects including this one.

1. Click the green “Clone or download” button, then press “Download ZIP”.

1. Right-click on the downloaded file, click “Extract All...”, then click “Extract”.

1. Open `Nibbles_VBGraphics-master\Nibbles_VBGraphics.sln`.
   After that you are free to tinker with the source.

_Note: exact wordings are different if your Windows installation is not in American English. The same applies for all instructions below._

## Programming styles

Check [Nibbles for VB.NET](https://github.com/lwchkg/nibbles_vbnet#programming-styles).
The coding style is essentially the same.

However, the line length is increased to 100, as 80 is found infeasible.

## Level set, sound, and copyright issues

As a VB.NET reincarnation of Nibbles, the original level set and sound are used in this program, sadly without authorization from Microsoft.

If the game gains momentum, then I will be discussing with Microsoft about the copyright issues.

For how the sound is made, check [Nibbles for VB.NET](https://github.com/lwchkg/nibbles_vbnet#level-set-sound-and-copyright-issues) for details.

## Difference from the original QBasic game

* The code is completely rewritten.

* Levels are made more symmetric.
  Both players have equivalent start positions at all times.
  In particular, if not moving, both players will die at the same time.

* If player’s head collide, both die and they do not score even if they hit the number.
  In the QBasic game, Sammy (player 1) gets the score. The players still die unless they are hitting a “9”.

* If both players are hitting the number at the same time, a random player gets the score if their heads don’t collide.
  In the QBasic game, Sammy (player 1) always get the score.

* It is possible to have one player levelling up and the other dying at the same time.
  In the QBasic game, levlling up makes the other snake not to die.

* The bug of the rotating stars in the introduction screen is fixed.
