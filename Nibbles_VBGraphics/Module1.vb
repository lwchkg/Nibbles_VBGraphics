' Copyright Wing-chung Leung 2018, except for level design and sound, which is owned by Microsoft.
' All rights reserved. Use of this source code is governed by a GPL-v3 license that can be found in
' the LICENSE file.

Option Strict On

Imports VBGraphics
Imports VBGraphics.Shapes
Imports VBGraphics.Text

Imports Color = System.Drawing.Color
Imports FontStyle = System.Drawing.FontStyle
Imports Point = System.Drawing.Point
Imports Size = System.Drawing.Size
Imports KeyEventArgs = System.Windows.Forms.KeyEventArgs
Imports Keys = System.Windows.Forms.Keys
Imports TextFormatFlags = System.Windows.Forms.TextFormatFlags

Module Module1

    Enum FieldObject
        Background
        Snake1
        Snake2
        Wall
    End Enum

    Enum Direction
        Null
        Up
        Right
        South
        Left
    End Enum

    Enum LevelSelection
        GoToLevelOne
        RemainInSameLevel
        GoToNextLevel
    End Enum

    Structure GameConfig
        Dim numPlayers As Integer
        Dim gameSpeed As Integer
        Dim increaseSpeedDuringPlay As Boolean
    End Structure

    Structure SnakeData
        Dim Body As Queue(Of Point)
        Dim TargetLength As Integer
        Dim Position As Point
        Dim Directions As List(Of Direction)
        Dim LivesLeft As Integer
        Dim Score As Integer
        Dim IsAlive As Boolean
    End Structure

    Structure GameColors
        Shared ReadOnly Background As Color = Color.DarkBlue
        Shared ReadOnly Snake1 As Color = Color.Yellow
        Shared ReadOnly Snake2 As Color = Color.Magenta
        Shared ReadOnly Wall As Color = Color.Red
        Shared ReadOnly Score As Color = Color.White
        Shared ReadOnly DialogForeground As Color = Color.White
        Shared ReadOnly DialogBackground As Color = Color.DarkRed
        Shared ReadOnly Number As Color = Color.Yellow
        Shared ReadOnly Sparkle As Color = Color.DarkMagenta
    End Structure

    Const maxNumberInLevel As Integer = 9
    Const maxGameSpeed As Integer = 100
    Const speedIncreasePerNumber As Integer = 2
    Const numberOfLevels As Integer = 10
    Const livesInGame As Integer = 5

    Const scoreMultiplierOfNumbers As Integer = 100
    Const lengthMultiplierOfNumbers As Integer = 4
    Const scorePenaltyForCrash As Integer = 1000

    Const fontName As String = "Arial"

    ReadOnly colorTable() As Color = {
        GameColors.Background,
        GameColors.Snake1,
        GameColors.Snake2,
        GameColors.Wall
    }

    Dim gw As GraphicsWindow
    Dim gameField(80 - 1, 50 - 1) As FieldObject
    Dim currentFrameInterval As TimeSpan
    Dim timeForNextFrame As Date

    ' Shows a flashing bulb patten on the screen which ends with a key press.
    Sub PauseWithFlashingBulbs(left As Integer, top As Integer,
                               width As Integer, height As Integer,
                               Optional bulbSize As Integer = 10,
                               Optional bulbDistance As Integer = 20)
        ' Index 0 - 2: colors when not highlighted.
        ' Index 3 - 5: colors when highlighted.
        Dim fillColors As Color() = {
            Color.FromArgb(&HFF600000),
            Color.FromArgb(&HFF606000),
            Color.FromArgb(&HFF006000),
            Color.FromArgb(&HFFD00000),
            Color.FromArgb(&HFFD0D000),
            Color.FromArgb(&HFF00D000)
        }

        Dim strokeColors As Color() = {
            Color.FromArgb(&HFF800000),
            Color.FromArgb(&HFF808000),
            Color.FromArgb(&HFF008000),
            Color.FromArgb(&HFFFF0000),
            Color.FromArgb(&HFFFFFF00),
            Color.FromArgb(&HFF00FF00)
        }

        Const numColors As Integer = 3

        Dim frameDuration As TimeSpan = TimeSpan.FromMilliseconds(800)

        Dim hSize As Integer = (width + bulbDistance - bulbSize) \ bulbDistance
        Dim vSize As Integer = (height + bulbDistance - bulbSize) \ bulbDistance

        gw.EmptyKeys()

        Dim originalBitmap As New Drawing.Bitmap(gw.Image)
        Dim indexToHighlight As Integer = 0
        Dim timeForNextFrame As Date = Now()

        Do Until gw.KeyAvailable
            If Now() < timeForNextFrame Then
                System.Threading.Thread.Sleep(10)
                Continue Do
            Else
                timeForNextFrame = Now() + frameDuration
            End If

            ' Draw on a fresh copy of the original image. Otherwise there will be problems related
            ' to antialiasing of the circles.
            gw.DrawImage(originalBitmap, 0, 0)

            ' Top bulbs.
            For i As Integer = 0 To hSize - 1
                Dim index As Integer = i Mod numColors
                If index = indexToHighlight Then index += numColors
                gw.DrawEllipseWithSmoothing(left + bulbDistance * i, top, bulbSize, bulbSize,
                                            fillColors(index), strokeColors(index))
            Next

            ' Right bulbs.
            Dim right As Integer = left + bulbDistance * (hSize - 1)
            For i As Integer = 1 To vSize - 2
                Dim index As Integer = (i + hSize + 2) Mod numColors
                If index = indexToHighlight Then index += numColors
                gw.DrawEllipseWithSmoothing(right, top + i * bulbDistance, bulbSize, bulbSize,
                                            fillColors(index), strokeColors(index))
            Next

            ' Bottom bulbs.
            Dim bottom As Integer = top + bulbDistance * (vSize - 1)
            For i As Integer = 0 To hSize - 1
                Dim index As Integer = (i + hSize + vSize + 1) Mod numColors
                If index = indexToHighlight Then index += numColors
                gw.DrawEllipseWithSmoothing(left + bulbDistance * (hSize - i - 1), bottom, bulbSize,
                                            bulbSize, fillColors(index), strokeColors(index))
            Next

            ' Left bulbs.
            For i As Integer = 1 To vSize - 2
                Dim index As Integer = (i + hSize * 2 + vSize) Mod numColors
                If index = indexToHighlight Then index += numColors
                gw.DrawEllipseWithSmoothing(left, top + bulbDistance * (vSize - i - 1), bulbSize,
                                            bulbSize, fillColors(index), strokeColors(index))
            Next

            indexToHighlight = (indexToHighlight + 1) Mod numColors

            System.Threading.Thread.Sleep(10)
        Loop
        gw.EmptyKeys()
    End Sub

    Sub DrawText(x As Integer, y As Integer, text As String,
                 Optional textColor As Color? = Nothing,
                 Optional emSize As Integer = 12,
                 Optional fontStyle As FontStyle = FontStyle.Regular)
        Dim color As Color = If(textColor.HasValue, textColor.Value, Color.LightGray)
        gw.DrawText(text, x, y, color, fontName, emSize, fontStyle)
    End Sub

    Sub DrawTextCentered(y As Integer, text As String,
                         Optional textColor As Color? = Nothing,
                         Optional emSize As Integer = 12,
                         Optional fontStyle As FontStyle = FontStyle.Regular)
        Dim color As Color = If(textColor.HasValue, textColor.Value, Color.LightGray)
        gw.DrawTextInRectangle(text, 50, y, 700, 500, color, fontName, emSize, fontStyle,
                               TextFormatFlags.HorizontalCenter Or TextFormatFlags.WordBreak)
    End Sub

    Sub ShowGameIntro()
        DrawTextCentered(70, "VBGraphics Nibbles", Color.White, 24,
                         FontStyle.Bold)
        DrawTextCentered(122, "A remake of QBasic Nibbles (1991) with VBGraphics")
        DrawTextCentered(142, "Copyright (C) Wing-chung Leung 2018", , 9)
        DrawTextCentered(170, "Note: QBasic Nibbles is a property of Microsoft Corporation", , 9)

        DrawTextCentered(198, "Navigate your snakes to eat numbers.  When you eat a number, " +
                              "you gain points and your snake becomes longer. Avoid running into " +
                              "anything else, i.e. walls, your snake, and the other snake.")

        DrawTextCentered(258, "Game Controls", Color.White,, FontStyle.Bold)

        gw.DrawTextInRectangle("Player 1", 344, 290, 120, 20, Color.LightGray, fontName, 12, ,
                               TextFormatFlags.HorizontalCenter)
        gw.DrawImage(My.Resources.player1keys, 344, 321)

        gw.DrawTextInRectangle("Player 2 (optional)", 549, 290, 150, 20, Color.LightGray, fontName,
                               12, , TextFormatFlags.HorizontalCenter)
        gw.DrawImage(My.Resources.player2keys, 564, 321)

        DrawText(175, 328, "Pause")
        gw.DrawImage(My.Resources.generalkeys, 136, 321)

        DrawTextCentered(410, "   Press any key to continue...")

        Call New System.Media.SoundPlayer(My.Resources.SoundStartGame).Play()
        PauseWithFlashingBulbs(35, 35, 730, 430)
    End Sub

    Function GetGameConfig() As GameConfig
        Dim forecolorDuringEntry As Color = Color.Yellow
        Dim backcolorDuringEntry As Color = Color.DarkBlue
        Dim forecolorAfterEntry As Color = Color.LightGray
        Dim backcolorAfterEntry As Color = Color.FromArgb(&HFF303030)

        Dim config As GameConfig

        gw.Clear(Color.Black)

        DrawText(190, 40, "How many players? (1 or 2)")
        gw.DrawRectangle(440, 40, 100, 19, backcolorDuringEntry, Nothing)
        Do
            Select Case gw.ReadKey().KeyCode
                Case Keys.D1, Keys.NumPad1
                    config.numPlayers = 1
                    Exit Do
                Case Keys.D2, Keys.NumPad2
                    config.numPlayers = 2
                    Exit Do
            End Select
        Loop
        gw.DrawRectangle(440, 40, 100, 19, backcolorAfterEntry, Nothing)
        DrawText(440, 40, config.numPlayers.ToString(), forecolorAfterEntry,, FontStyle.Bold)

        DrawText(190, 100,
                 "Skill level? (1 to 100)" + vbCrLf +
                 "    1   = Novice" + vbCrLf +
                 "    90  = Expert" + vbCrLf +
                 "    100 = Twiddle Fingers" + vbCrLf +
                 "(Computer speed may affect your skill level)")
        Do
        Loop Until _
            Integer.TryParse(gw.ReadLine(440, 100, 100, forecolorDuringEntry, backcolorDuringEntry,
                                         fontName, 12, FontStyle.Bold, forecolorAfterEntry,
                                         backcolorAfterEntry),
                             config.gameSpeed) AndAlso
            config.gameSpeed >= 1 AndAlso config.gameSpeed <= 100

        DrawText(100, 240, "Increase game speed during play? (Y or N) ")
        gw.DrawRectangle(440, 240, 100, 19, backcolorDuringEntry, Nothing)
        Do
            Select Case gw.ReadKey().KeyCode
                Case Keys.Y
                    config.increaseSpeedDuringPlay = True
                    Exit Do
                Case Keys.N
                    config.increaseSpeedDuringPlay = False
                    Exit Do
            End Select
        Loop
        gw.DrawRectangle(440, 240, 100, 19, backcolorAfterEntry, Nothing)
        DrawText(440, 40, If(config.increaseSpeedDuringPlay, "Y", "N"), forecolorAfterEntry, ,
                 FontStyle.Bold)

        gw.EmptyKeys()

        Return config
    End Function

    Sub DisplayPointInField(left As Integer, top As Integer)
        gw.DrawRectangle(left * 10, top * 10, 10, 10, colorTable(gameField(left, top)), Nothing)
    End Sub

    Sub SetPointInField(left As Integer, top As Integer, obj As FieldObject)
        If gameField(left, top) = obj Then Return

        gameField(left, top) = obj
        DisplayPointInField(left, top)
    End Sub

    Sub AddWallToField(left As Integer, top As Integer)
        SetPointInField(left, top, FieldObject.Wall)
    End Sub

    Sub ClearField()
        gw.Clear(GameColors.Background)

        For top As Integer = 0 To 49
            For left As Integer = 0 To 79
                gameField(left, top) = FieldObject.Background
            Next
        Next
    End Sub

    ' Shows a dialog and wait for one of the specified keys, or any key if no key is specified. The
    ' text is center-aligned in the dialog.
    Function ShowDialog(text As String, keysToContinue() As Keys) As KeyEventArgs
        Dim font = New Drawing.Font(fontName, 12, FontStyle.Bold)
        Dim size As Size = Windows.Forms.TextRenderer.MeasureText(text, font)
        Dim left1 As Integer = 400 - size.Width \ 2
        Dim top1 As Integer = 220 - size.Height \ 2
        gw.DrawRectangle(left1 - 15, top1 - 15, size.Width + 30, size.Height + 30,
                         GameColors.DialogForeground, Nothing)
        gw.DrawRectangle(left1 - 10, top1 - 10, size.Width + 20, size.Height + 20,
                         GameColors.DialogBackground, Nothing)
        gw.DrawTextInRectangle(text, left1, top1, size.Width, size.Height,
                               GameColors.DialogForeground, font, TextFormatFlags.HorizontalCenter)

        gw.EmptyKeys()
        Dim key As KeyEventArgs
        Do
            key = gw.ReadKey()
        Loop Until keysToContinue.Count = 0 OrElse keysToContinue.Contains(key.KeyCode)
        gw.EmptyKeys()

        ' Restore the screen background.
        For y As Integer = (top1 - 15) \ 10 To (top1 + size.Height + 14) \ 10
            For x As Integer = (left1 - 15) \ 10 To (left1 + size.Width + 14) \ 10
                DisplayPointInField(x, y)
            Next
        Next

        Return key
    End Function

    Sub ShowLevelIntro(level As Integer)
        ShowDialog(String.Format("Level {0},  Push Space", level), {Keys.Space})
    End Sub

    Sub PrintLivesAndScore(numPlayers As Integer, snakes() As SnakeData)
        gw.DrawRectangle(0, 0, 800, 20, GameColors.Background, Nothing)
        gw.DrawTextInRectangle(String.Format("SAMMY    Lives: {0}    Score: {1:N0}",
                                             snakes(0).LivesLeft, snakes(0).Score),
                               400, 0, 400, 20, GameColors.Score, fontName, 12, FontStyle.Bold,
                               Windows.Forms.TextFormatFlags.Right)

        If numPlayers < 2 Then Return

        gw.DrawTextInRectangle(String.Format("Score: {1:N0}    Lives: {0}    JAKE",
                                             snakes(1).LivesLeft, snakes(1).Score),
                               0, 0, 400, 20, GameColors.Score, fontName, 12, FontStyle.Bold)
    End Sub

    Sub InitializeLevel(level As Integer, numPlayers As Integer, snakes() As SnakeData)
        For i As Integer = 0 To 1
            snakes(i).IsAlive = True
            snakes(i).TargetLength = 2
            snakes(i).Body = New Queue(Of Point)
            snakes(i).Directions = New List(Of Direction)
        Next

        ClearField()

        ' Create outside border
        For left As Integer = 0 To 79
            AddWallToField(left, 2)
            AddWallToField(left, 49)
        Next
        For top As Integer = 3 To 48
            AddWallToField(0, top)
            AddWallToField(79, top)
        Next

        ' Create interior of a level, and specify the positions and directions of the snakes.
        Select Case level
            Case 1
                snakes(0).Position = New Point(49, 25)
                snakes(0).Directions.Add(Direction.Right)
                snakes(1).Position = New Point(30, 25)
                snakes(1).Directions.Add(Direction.Left)
            Case 2
                For left As Integer = 19 To 59
                    AddWallToField(left, 24)
                Next
                snakes(0).Position = New Point(59, 8)
                snakes(0).Directions.Add(Direction.Left)
                snakes(1).Position = New Point(20, 42)
                snakes(1).Directions.Add(Direction.Right)
            Case 3
                For top As Integer = 9 To 39
                    AddWallToField(20, top)
                    AddWallToField(59, top)
                Next
                snakes(0).Position = New Point(49, 26)
                snakes(0).Directions.Add(Direction.Up)
                snakes(1).Position = New Point(30, 25)
                snakes(1).Directions.Add(Direction.South)
            Case 4
                For i As Integer = 3 To 29
                    AddWallToField(20, i)
                    AddWallToField(59, 51 - i)
                Next
                For i As Integer = 1 To 39
                    AddWallToField(i, 37)
                    AddWallToField(79 - i, 14)
                Next
                snakes(0).Position = New Point(59, 8)
                snakes(0).Directions.Add(Direction.Left)
                snakes(1).Position = New Point(20, 42)
                snakes(1).Directions.Add(Direction.Right)
            Case 5
                For left As Integer = 22 To 56
                    AddWallToField(left, 10)
                    AddWallToField(left, 41)
                Next
                For top As Integer = 12 To 39
                    AddWallToField(20, top)
                    AddWallToField(58, top)
                Next
                snakes(0).Position = New Point(49, 26)
                snakes(0).Directions.Add(Direction.Up)
                snakes(1).Position = New Point(29, 25)
                snakes(1).Directions.Add(Direction.South)
            Case 6
                For top As Integer = 3 To 48
                    If top >= 22 AndAlso top <= 29 Then Continue For
                    For left As Integer = 9 To 69 Step 10
                        AddWallToField(left, top)
                    Next
                Next top
                snakes(0).Position = New Point(64, 6)
                snakes(0).Directions.Add(Direction.South)
                snakes(1).Position = New Point(14, 45)
                snakes(1).Directions.Add(Direction.Up)
            Case 7
                For top As Integer = 4 To 49 Step 2
                    AddWallToField(39, top)
                Next
                snakes(0).Position = New Point(64, 6)
                snakes(0).Directions.Add(Direction.South)
                snakes(1).Position = New Point(14, 45)
                snakes(1).Directions.Add(Direction.Up)
            Case 8
                For i As Integer = 3 To 39
                    AddWallToField(9, i)
                    AddWallToField(19, 51 - i)
                    AddWallToField(29, i)
                    AddWallToField(39, 51 - i)
                    AddWallToField(49, i)
                    AddWallToField(59, 51 - i)
                    AddWallToField(69, i)
                Next
                snakes(0).Position = New Point(64, 6)
                snakes(0).Directions.Add(Direction.South)
                snakes(1).Position = New Point(14, 45)
                snakes(1).Directions.Add(Direction.Up)
            Case 9
                For i As Integer = 5 To 46
                    AddWallToField(i, i)
                    AddWallToField(i + 28, i)
                Next
                snakes(0).Position = New Point(74, 39)
                snakes(0).Directions.Add(Direction.Up)
                snakes(1).Position = New Point(5, 12)
                snakes(1).Directions.Add(Direction.South)
            Case Else
                For i As Integer = 3 To 48 Step 2
                    AddWallToField(9, i)
                    AddWallToField(19, i + 1)
                    AddWallToField(29, i)
                    AddWallToField(39, i + 1)
                    AddWallToField(49, i)
                    AddWallToField(59, i + 1)
                    AddWallToField(69, i)
                Next
                snakes(0).Position = New Point(64, 6)
                snakes(0).Directions.Add(Direction.South)
                snakes(1).Position = New Point(14, 45)
                snakes(1).Directions.Add(Direction.Up)
        End Select

        ' Disable the second snake in one-player mode.
        If numPlayers = 1 Then
            snakes(1).Position = New Point(-1, -1)
        End If
    End Sub

    Sub SetGameSpeed(speed As Integer, isRelative As Boolean)
        Static currentGameSpeed As Integer = 1
        currentGameSpeed = Math.Min(If(isRelative, currentGameSpeed + speed, speed), maxGameSpeed)
        currentFrameInterval = New TimeSpan(0, 0, 0, 0, 121 - currentGameSpeed)
    End Sub

    Sub SetTimeForNextFrame(forcedReset As Boolean)
        Dim now As Date = Date.Now()
        If forcedReset Then
            timeForNextFrame = now + currentFrameInterval
        Else
            timeForNextFrame += currentFrameInterval
            ' Reset time if lagged too much, which implies the game process has been paused.
            If timeForNextFrame < now Then
                timeForNextFrame = now + currentFrameInterval
            End If
        End If
    End Sub

    Sub InitializeRound(level As Integer, showIntro As Boolean, numPlayers As Integer,
                        snakes() As SnakeData)
        InitializeLevel(level, numPlayers, snakes)
        PrintLivesAndScore(numPlayers, snakes)
        If showIntro Then
            ShowLevelIntro(level)
        End If
        SetTimeForNextFrame(True)
    End Sub

    ' Add new directions to the snakes according to user input, if the new directions do not move a
    ' snake backward. Each direction In the queue lasts for one move except for the last inputted
    ' direction.
    Sub InputSnakeDirections(numPlayers As Integer, snakes() As SnakeData)
        Do While gw.KeyAvailable()
            Dim key As System.Windows.Forms.KeyEventArgs = gw.ReadKey()
            Select Case key.KeyCode
                Case Keys.Up, Keys.NumPad8
                    If snakes(0).Directions.Last <> Direction.South Then
                        snakes(0).Directions.Add(Direction.Up)
                    End If
                Case Keys.Left, Keys.NumPad4
                    If snakes(0).Directions.Last <> Direction.Right Then
                        snakes(0).Directions.Add(Direction.Left)
                    End If
                Case Keys.Down, Keys.NumPad2
                    If snakes(0).Directions.Last <> Direction.Up Then
                        snakes(0).Directions.Add(Direction.South)
                    End If
                Case Keys.Right, Keys.NumPad6
                    If snakes(0).Directions.Last <> Direction.Left Then
                        snakes(0).Directions.Add(Direction.Right)
                    End If
                Case Keys.P
                    ShowDialog("Game Paused... Push Space", {Keys.Space})
                    SetTimeForNextFrame(True)
            End Select

            If numPlayers = 1 Then Continue Do

            Select Case key.KeyCode
                Case Keys.W
                    If snakes(1).Directions.Last <> Direction.South Then
                        snakes(1).Directions.Add(Direction.Up)
                    End If
                Case Keys.A
                    If snakes(1).Directions.Last <> Direction.Right Then
                        snakes(1).Directions.Add(Direction.Left)
                    End If
                Case Keys.S
                    If snakes(1).Directions.Last <> Direction.Up Then
                        snakes(1).Directions.Add(Direction.South)
                    End If
                Case Keys.D
                    If snakes(1).Directions.Last <> Direction.Left Then
                        snakes(1).Directions.Add(Direction.Right)
                    End If
            End Select
        Loop
    End Sub

    ' Move both snake heads according to the directions of the snakes. If more than one direction
    ' is stored, remove the first one as it expires.
    Sub MoveSnakeHeads(numPlayers As Integer, snakes() As SnakeData)
        For i As Integer = 0 To numPlayers - 1
            If snakes(i).Directions.Count > 1 Then
                snakes(i).Directions.RemoveAt(0)
            End If
            Select Case snakes(i).Directions.First
                Case Direction.Up
                    snakes(i).Position += New Size(0, -1)
                Case Direction.Right
                    snakes(i).Position += New Size(1, 0)
                Case Direction.South
                    snakes(i).Position += New Size(0, 1)
                Case Else
                    snakes(i).Position += New Size(-1, 0)
            End Select
        Next
    End Sub

    ' Kill both snakes if the heads collide. Returns whether the snakes are killed.
    Function KillSnakesIfHeadsCollide(numPlayers As Integer, snakes() As SnakeData) As Boolean
        If numPlayers = 2 AndAlso snakes(0).Position = snakes(1).Position Then
            snakes(0).IsAlive = False
            snakes(1).IsAlive = False
            Return True
        End If
        Return False
    End Function

    ' Generate the specified number, display it on the screen, and returns its position.
    Sub GenerateNumber(number As Integer, ByRef left As Integer,
                       ByRef screenTop As Integer)
        Do
            left = CInt(Int(Rnd() * 78)) + 1
            screenTop = CInt(Int(Rnd() * 22)) + 2
        Loop Until gameField(left, screenTop * 2) = FieldObject.Background AndAlso
                   gameField(left, screenTop * 2 + 1) = FieldObject.Background

        gw.DrawText(number.ToString(), left * 10 - 4, screenTop * 20 + 2, GameColors.Number,
                    fontName, 12, FontStyle.Bold)
    End Sub

    ' Check if the snakes hits the numbers. If yes add to score, increase length of snake, increase
    ' speed (if appropriate) and play sound. If both snakes hit the number, decide who gets the
    ' number randomly.
    Sub ScoreIfHitNumber(config As GameConfig, snakes() As SnakeData,
                         ByRef numberScreenTop As Integer,
                         ByRef numberLeft As Integer,
                         ByRef nextNumberToHit As Integer,
                         ByRef isRoundWon As Boolean)
        Dim snakeHittingNumber As Integer = -1
        For i = 0 To config.numPlayers - 1
            If snakes(i).Position.X = numberLeft AndAlso
               snakes(i).Position.Y \ 2 = numberScreenTop AndAlso
               (i = 0 OrElse snakeHittingNumber <> 0 OrElse Rnd() < 0.5F) Then
                snakeHittingNumber = i
            End If
        Next

        If snakeHittingNumber >= 0 Then
            snakes(snakeHittingNumber).TargetLength += nextNumberToHit * lengthMultiplierOfNumbers
            snakes(snakeHittingNumber).Score += nextNumberToHit * scoreMultiplierOfNumbers
            PrintLivesAndScore(config.numPlayers, snakes)

            If config.increaseSpeedDuringPlay Then
                SetGameSpeed(speedIncreasePerNumber, True)
            End If

            nextNumberToHit += 1
            If nextNumberToHit > maxNumberInLevel Then
                isRoundWon = True
            Else
                DisplayPointInField(numberLeft, numberScreenTop * 2)
                DisplayPointInField(numberLeft, numberScreenTop * 2 + 1)
                GenerateNumber(nextNumberToHit, numberLeft, numberScreenTop)
            End If

            Call New System.Media.SoundPlayer(My.Resources.SoundHitNumber).Play()
        End If
    End Sub

    Sub MoveSnakeBodiesOrKillSnake(numPlayers As Integer, snakes() As SnakeData)
        ' Erase trails if already at target length.
        For i = 0 To numPlayers - 1
            If snakes(i).Body.Count >= snakes(i).TargetLength Then
                Dim pointToRemove As Point = snakes(i).Body.Dequeue()
                SetPointInField(pointToRemove.X, pointToRemove.Y, FieldObject.Background)
            End If
        Next

        ' Add snake head to the body, or kill it because of crash.
        For i As Integer = 0 To numPlayers - 1
            If gameField(snakes(i).Position.X, snakes(i).Position.Y) <> FieldObject.Background Then
                snakes(i).IsAlive = False
            Else
                snakes(i).Body.Enqueue(snakes(i).Position)
                SetPointInField(snakes(i).Position.X, snakes(i).Position.Y,
                                If(i = 0, FieldObject.Snake1, FieldObject.Snake2))
            End If
        Next
    End Sub

    ' Show the animation for erasing the snakes.
    Sub EraseSnakes(numPlayers As Integer, snakes() As SnakeData)
        Const animationSteps As Integer = 10
        Const animationFrameTime As Integer = 30  ' unit = millisecond

        Dim snakeBody(numPlayers - 1)() As Point
        Dim snakeIndex(numPlayers - 1) As Integer
        For i As Integer = 0 To numPlayers - 1
            snakeBody(i) = snakes(i).Body.ToArray()
            snakeIndex(i) = snakeBody(i).Count
        Next

        Dim actualSteps As Integer = Math.Min(animationSteps, snakeIndex.Max())
        For frameNumber As Integer = 0 To actualSteps - 1
            For i As Integer = 0 To numPlayers - 1
                snakeIndex(i) -= 1
                For j As Integer = snakeIndex(i) To 0 Step -animationSteps
                    SetPointInField(snakeBody(i)(j).X, snakeBody(i)(j).Y, FieldObject.Background)
                Next
            Next

            ' Repaint the screen.
            gw.Form.Refresh()

            System.Threading.Thread.Sleep(animationFrameTime)
        Next
    End Sub

    ' Plays a round of nibbles. Returns if the round is won. The snakes' lives and score may also
    ' change.
    Function PlayRound(config As GameConfig, level As Integer,
                       showIntro As Boolean, snakes() As SnakeData) As Boolean
        InitializeRound(level, showIntro, config.numPlayers, snakes)
        Call New System.Media.SoundPlayer(My.Resources.SoundStartRound).Play()

        Dim isRoundWon As Boolean = False
        Dim numberScreenTop As Integer
        Dim numberLeft As Integer
        Dim nextNumberToHit As Integer = 1
        GenerateNumber(nextNumberToHit, numberLeft, numberScreenTop)
        Do
            System.Threading.Thread.Sleep(0)

            InputSnakeDirections(config.numPlayers, snakes)

            If Date.Now < timeForNextFrame Then
                Continue Do
            Else
                SetTimeForNextFrame(False)
            End If

            MoveSnakeHeads(config.numPlayers, snakes)

            ' Check if the snakes' heads collide. In this case both snakes
            ' die, the snakes do not score for hitting a number and the
            ' level does not advance.
            If KillSnakesIfHeadsCollide(config.numPlayers, snakes) Then
                Exit Do
            End If

            ScoreIfHitNumber(config, snakes, numberScreenTop, numberLeft, nextNumberToHit,
                             isRoundWon)

            ' Move the snakes even if the level is already won. The other snake can be killed at the
            ' same time.
            MoveSnakeBodiesOrKillSnake(config.numPlayers, snakes)
        Loop While Not isRoundWon AndAlso snakes(0).IsAlive AndAlso snakes(1).IsAlive

        For i = 0 To config.numPlayers - 1
            If Not snakes(i).IsAlive Then
                snakes(i).Score -= scorePenaltyForCrash
                snakes(i).LivesLeft -= 1
                SetGameSpeed(config.gameSpeed, False)
            End If
        Next

        PrintLivesAndScore(config.numPlayers, snakes)
        If Not snakes(0).IsAlive OrElse Not snakes(1).IsAlive Then
            Call New System.Media.SoundPlayer(My.Resources.SoundSnakeDie).Play()
        End If
        EraseSnakes(config.numPlayers, snakes)
        Return isRoundWon
    End Function

    ' Play nibbles until a player running out of lives.
    Sub PlayGame(config As GameConfig)
        Dim snakes(2 - 1) As SnakeData
        For i As Integer = 0 To 1
            snakes(i).LivesLeft = livesInGame
            snakes(i).Score = 0
        Next

        Dim currentLevel As Integer = 1
        Dim showIntro As Boolean = True
        SetGameSpeed(config.gameSpeed, False)
        Do
            Dim isRoundWon As Boolean = PlayRound(config, currentLevel, showIntro, snakes)
            If isRoundWon Then
                currentLevel += 1
            End If

            ' If the round is won, show the introduction of the new level. Otherwise there is a
            ' dialog for losing a life.
            showIntro = isRoundWon

            ' It is possible to have one snake dying and the other advancing to the next level at
            ' the same time. In this case both dialogs are shown if the lives are not used up.
            If Not snakes(0).IsAlive OrElse Not snakes(1).IsAlive Then
                Dim message As String =
                    If(snakes(0).IsAlive, "Jake Died!",
                       If(snakes(1).IsAlive, "Sammy Died!", "Both Died!")) +
                    ControlChars.CrLf + ControlChars.CrLf + "Push Space"
                ShowDialog(message, {Keys.Space})
            End If
        Loop While snakes(0).LivesLeft > 0 AndAlso snakes(1).LivesLeft > 0
    End Sub

    Function StillWantsToPlay() As Boolean
        Dim key As KeyEventArgs = ShowDialog(
            "G A M E   O V E R" & vbCrLf & vbCrLf & "Play Again? (Y/N)", {Keys.Y, Keys.N})
        Return key.KeyCode = Keys.Y
    End Function

    Sub Main()
        gw = New GraphicsWindow(800, 500)
        gw.CanClose = True
        gw.Form.Text = "VBGraphics Nibbles"

        ShowGameIntro()
        Dim config As GameConfig = GetGameConfig()
        Do
            PlayGame(config)
        Loop While StillWantsToPlay()

        gw.Dispose()
    End Sub

End Module
