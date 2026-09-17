# RL Air Hockey Self-Play

> Physics-based air hockey self-play in Unity ML-Agents with fully symmetric observations and a single shared policy.

A reinforcement learning project where a single neural network controls both sides of an air hockey match in Unity.

The environment was designed so that both agents receive the same local representation of the game state regardless of whether they play as blue or red. This makes the observation space side-invariant and allows one shared policy to learn from both halves of the table.

---

# Project Highlights

- Single shared PPO policy for both agents
- Fully symmetric / side-invariant observation space
- 15 vector observations
- 8 boundary raycasts for arena orientation
- Continuous control in local coordinates
- Physics-based puck, mallets, collisions and friction
- One episode produces experience from both sides
- Minimal reward shaping
- No curriculum learning
- Multiple long training runs with 10M+ steps
- Emergent behaviors discovered during training
- Human vs AI evaluation
- Unity ML-Agents + PPO

---

# Overview

The goal of this project was to investigate whether convincing air hockey behavior can emerge from a deliberately minimal setup.

Instead of building a large reward stack or giving the agent many hand-crafted tactical cues, the environment was kept compact:

- the same policy controls both players,
- all observations are transformed into the agent's local frame,
- rewards are intentionally basic,
- and learning happens only through physics interaction and self-play.

The central idea is not simply that one policy plays both sides, but that the two sides become indistinguishable from the policy's point of view. The agent should learn how to play, not which side it occupies.

### Environment preview

![Air Hockey environment](media/environment_preview.gif)

*A short uncut exchange between two agents controlled by the same policy.*

---

# Demo

## Final policy playing in the arena

![Final policy gameplay 1](media/final_policy_1.gif)

![Final policy gameplay 2](media/final_policy_2.gif)

A full evaluation video without cuts or scripted actions is available on YouTube.

The project was also tested in a human-vs-agent match. The human player was able to compete, but the result still showed that the learned controller is highly effective for this type of continuous, physics-based control task.

---

# Environment Design

The environment is built around a physics-based air hockey table with two mallets, one puck and two goals.

Several design decisions were made to keep the learning problem symmetric:

- both agents share one neural network,
- observations are expressed in the agent's local coordinate system,
- velocities are also transformed locally,
- the same action space is used for both sides,
- and arena boundaries are sensed only for orientation.

This means the policy does not receive an explicit team identifier or any direct clue about whether it is controlling the blue or red side.

The use of a single policy is primarily a design choice. It makes the setup compact, keeps the self-play loop visually interesting, and lets the same network continuously improve against both sides of itself.

---

# Observation Space

The policy receives a 15-dimensional vector observation from the `CollectObservations` method.

The observation set is built from relative local positions, relative local velocities, gate positions and self velocity. In addition, the agent uses 8 raycasts placed on a separate layer to sense only the arena boundaries and estimate its orientation inside the table.

The raycasts are not used to locate the puck or the opponent. Their role is only to provide boundary awareness.

The important point is that the observations are side-invariant. The same physical situation is represented identically for both agents after transformation into local space.

This was a central design goal of the project.

### Symmetric local representation

![Symmetric observations](media/symmetric_observations.png)

*Both agents see the game through their own local coordinate frame, so equivalent situations on opposite sides produce the same observation semantics.*

| Observation                     | Source                                         |
| :------------------------------ | :--------------------------------------------- |
| Relative position of puck       | local coordinates                              |
| Relative velocity of puck       | local coordinates                              |
| Relative position of opponent   | local coordinates                              |
| Relative velocity of opponent   | local coordinates                              |
| Relative position of own gate   | local coordinates                              |
| Relative position of enemy gate | local coordinates                              |
| Self velocity                   | local coordinates                              |
| Is touching puck?               | binary flag                                    |
| **8 raycasts**                  | distance and object type (arena boundary only) |

---

# Action Space

Continuous action space consisting of two outputs:

| Action | Description              |
| ------ | ------------------------ |
| X      | local forward/back force |
| Z      | local lateral force      |

The actions are applied in the agent's local coordinate system through relative force, so both sides are controlled in the same way.

This makes the controller feel physically grounded rather than scripted or teleported.

---

# Reward Design

The final reward function was kept intentionally minimal.

| Reward / Penalty                            | Value                            |
| :------------------------------------------ | :------------------------------- |
| Scoring a goal                              | `+1`, `+2`, `+3`, `+10`          |
| Conceding a goal                            | `-1`, `-2`, `-3`, `-10`          |
| Useful impulse (toward enemy goal)          | `Δspeed * 0.35`                  |
| Stillness penalty (puck static on own half) | `-0.06 / sec` (after 0.4s delay) |
| Home distance penalty                       | `-0.000525 * distance²`          |
| Timeout penalty (episode ends without goal) | `-0.5`                           |

The final reward function intentionally avoids complex tactical shaping and focuses on fundamental objectives.

One important design choice was to allow some movement away from the puck if needed, while still punishing excessive passive behavior. This helped avoid policies that simply stayed near the spawn point and did nothing.

---

# Training Runs

Several long training runs were completed, each revealing a different emergent strategy.

### Run 1 — ~20M steps

![Run 1](media/run1.gif)

This run produced the most unusual result. The policy developed different behavior for the two sides even though the observation space was designed to be symmetric.

The cause was a collision-related sign issue whose behavior was not stable across Unity restarts. In practice, this introduced unintended asymmetry into the reward feedback, and the policy learned side-dependent behavior from it.

### Run 2 — ~12M steps

![Run 2](media/run2.gif)

A more conventional policy.

The agents blocked, returned and attacked with reasonable frequency. This was the most visually normal version of the game.

### Run 3 — ~50M steps

![Run 3](media/run3.gif)

This longer run revealed a less human-like but still highly effective strategy.

Instead of ending rallies by scoring, the agents learned to keep the puck active by repeatedly redirecting it between each other and the walls near the goal area. The policy found a stable reward pattern in prolonged exchanges and avoided committing to risky shots that would terminate the rally.

### Run 4 — final

![Run 4](media/run4_final.gif)

This is the most successful run.

With a stronger goal reward, the policy developed the most convincing air hockey behavior so far. When the opponent had an opportunity to shoot, the defending agent would often shift back toward its own goal and position itself between the puck and the net. In practice, this looks like a simple blocking strategy: the agent does not just chase the puck, but tries to occupy the line of attack.

The result is not human control in the literal sense, but it is tactically coherent and visually close to how real air hockey is often played.

---

# Emergent Behaviors

Several behaviors were not explicitly programmed and appeared during training:

- aggressive puck chasing
- blocking the goal area
- redirecting the puck into walls
- moving between the puck and the agent's own goal to block a shot lane
- side-dependent strategy separation in the run affected by reward asymmetry in a symmetric observation environment

### Emergent defensive positioning

When the opponent gains a shooting opportunity, the agent often retreats toward its own goal and moves into the puck-goal line instead of simply continuing to chase the puck.

The most interesting behaviors were not scripted tactics, but simple policies that became stable through reward optimization. In particular, the final policy learned to retreat toward its own goal when the opponent had a clear shooting chance, which produces a defensive shape without any hard-coded positioning logic.

---

# Development Notes

A large part of the development time was spent on physical tuning and symmetry constraints rather than on the neural network itself.

The environment required careful adjustment of:

- collision behavior,
- friction,
- bounce response,
- linear damping,
- reset consistency,
- and invariant observation mapping.

A particularly important lesson was that even when observations look symmetric, small hidden asymmetries in physics or reward feedback can completely change the learned strategy.

One collision-related issue showed that the direction inferred from contact information was not stable enough to use as a semantic signal across Unity restarts. That made the reward interpretation unreliable and was one of the reasons the first long run produced an unusual side-dependent policy.

---

# Results

The trained policies show that a single shared network can learn to play both sides of an air hockey match with no explicit team identity in the observations.

The policy can:

- move in the correct local direction on both sides,
- intercept the puck,
- block shots,
- attack the opponent's goal,
- and adapt to different reward configurations.

The most successful version does not play like a human in a literal sense, but it does reproduce a recognizable tactical pattern: when a shot becomes dangerous, the agent shifts toward its own goal and tries to block the lane instead of blindly chasing the puck.

---

# Training Progress

TensorBoard graphs were recorded for multiple training runs.

Because the reward structure remained comparable across the final experiments, the curves are useful for comparing policies rather than only reward scale.

![Episode Length](media/Episode_Length.png)
![Cumulative Reward](media/Cumulative_Reward.png)

---

# Technologies

- Unity 6
- Unity ML-Agents
- C#
- PPO (Proximal Policy Optimization)
- Physics-based simulation
- TensorBoard
- Barracuda / ONNX

---

# Known Issues / Limitations

- The policy is tightly coupled to the current physics setup and may behave differently if friction, damping or collision parameters are changed.
- The environment is designed for symmetric self-play and does not use separate team-specific policies.
- Some long-run strategies are reward-driven rather than human-like.
- Small hidden asymmetries in physics or reward calculation can significantly influence the learned strategy, even when the observation space appears fully symmetric.
- The project was trained in simulation only.
- Human play against the agent is harder than it first looks because the agent is optimized for continuous force control, not for human-style discrete input.

---

# Play Against the Agent

The project can also be evaluated manually against the trained policy.

![Human vs AI](media/human_vs_ai.gif)

*Human-controlled mallet playing against the trained policy.*

---

# License

Released under the MIT License.
