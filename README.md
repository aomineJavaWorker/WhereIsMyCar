# 🚗 Where Is My Car?

**Where Is My Car?** is a 2D Unity game where the player must match different vehicles with their correct places on a city map.

Vehicles and their target places are randomly positioned around the map.  
The player can move, rotate, resize and flip vehicles until they match the correct position.

<img width="1114" height="628" alt="main-menu" src="https://github.com/user-attachments/assets/49c61bf5-48c9-4f17-a144-610d17427a26" />


---

## 🎮 Gameplay

The goal is to place every vehicle into the correct location.

Each vehicle has its own matching place. To place a vehicle successfully, the player must match:

- vehicle type;
- size;
- rotation;
- direction.

<img width="1114" height="623" alt="gameplay" src="https://github.com/user-attachments/assets/dba92b48-b011-4028-b73f-4bb3e104f847" />

### Placement rules

- **Correct vehicle + correct size + correct rotation** → the vehicle is placed.
- **Correct vehicle + incorrect size or rotation** → the vehicle stays where it was dropped.
- **Incorrect vehicle** → the vehicle returns to its original spawn position.
- **Vehicle dropped on the background** → it stays in the new position.

<img width="336" height="143" alt="vehicle-placement" src="https://github.com/user-attachments/assets/42933a5e-7f56-41ac-a493-748ff3220024" />


---

## 🧩 Hanojas Tornis (Tower of Hanoi)

The project also includes **Hanojas Tornis**, a separate puzzle game accessible from the main menu.

### How to play

Choose a difficulty of **4, 5 or 6 disks**. Move the whole stack from the first rod to the third rod by dragging one disk at a time. Only the top disk can be moved, and a larger disk cannot be placed on a smaller one.

- Drag a disk with the mouse or touchscreen to another rod.
- The move counter and timer track your progress.
- The victory screen displays your moves, time and a star rating.
- **Restart** resets the puzzle; **Main Menu** returns to the game selection screen.
- An optional **rewarded ad**, started by the player, unlocks a bonus holder for one disk.

The minimum number of moves is **15**, **31** or **63**, depending on the chosen difficulty.

### Screenshots

Real gameplay screenshots of the Hanoi scene still need to be captured in Unity and added here. Suggested screenshots: difficulty selection, moving the disks, and victory screen.


### Screenshots

#### Difficulty selection

Choose between 4, 5 or 6 disks.

![Hanoi difficulty selection](docs/images/hanojas-difficulty.png)

#### Gameplay

Move disks between three rods without placing a larger disk on a smaller one.

![Hanoi gameplay](docs/images/hanojas-gameplay.png)

#### Victory

Complete the puzzle to see your moves, time and star rating.

![Hanoi victory screen](docs/images/hanojas-victory.png)


---

## 🕹️ Controls

| Control | Action |
|---|---|
| Left Mouse Button | Drag vehicle |
| Z | Rotate vehicle left |
| X | Rotate vehicle right |
| ↑ / ↓ | Change vehicle height |
| ← / → | Change vehicle width |
| Space | Flip vehicle horizontally |
| Mouse movement | Move camera |
| Mouse Wheel | Zoom camera |
| On-screen + / - | Zoom on Android phones and emulators |

---

## ✨ Features

- Random vehicle and target placement
- Drag-and-drop vehicle movement
- Vehicle rotation
- Vehicle resizing
- Horizontal vehicle flipping
- Tag-based vehicle validation
- Individual spawn position for every vehicle
- Camera movement and zoom (mouse wheel, pinch, and on-screen buttons)
- Tower of Hanoi puzzle with three difficulty levels
- Unity Ads: automatic banner ads, interstitial ads, and player-initiated rewarded ads
- Sound effects
- Background music
- Animated main menu
- Animated Police and Robber vehicles
- Decorative moving vehicles
- Flying clouds and aircraft
- Bomb explosion system

---

## 💣 Flying Objects

The game also contains animated flying objects such as clouds, aircraft and bombs.

Flying objects:

- spawn at random heights;
- move across the screen;
- use different movement speeds;
- fade in and out.

Bombs react when the cursor touches them.

When activated, a bomb:

1. changes color;
2. starts vibrating;
3. plays an explosion animation and sound;
4. affects nearby flying objects.

<img width="294" height="218" alt="bomb-explosion" src="https://github.com/user-attachments/assets/5ea59b17-1fe5-482e-b802-fe4a6b19e265" />


---

## 🏠 Main Menu

The game contains an animated main menu.

The menu includes:

- animated **WHERE IS MY CAR?** title;
- short gameplay description;
- Start button;
- Exit button;
- hover animations;
- hover sound effects;
- background music;
- animated Police and Robber vehicles;
- additional decorative traffic;
- **HANOJAS TORNIS** game selection;
- automatic banner ads.

The title uses a subtle looping scale animation to make the menu feel more alive.

---

## 🛠️ Technologies

The project was developed using:

- **Unity 6**
- **Unity 6000.0.49f1**
- **C#**
- Unity UI
- TextMeshPro
- Unity Event System
- Animator
- 2D Physics

---

## 📁 Project Structure

```text
Assets/
├── animation/
├── Scenes/
│   ├── MainMenu.unity
│   ├── CityScene.unity
│   └── HanojasTornis.unity
├── Scripts/
├── Sounds/
├── Sprites/
└── ...

```
