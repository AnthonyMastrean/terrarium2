//------------------------------------------------------------------------------
//      Copyright (c) Microsoft Corporation.  All rights reserved.                                                             
//------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;

namespace OrganismBase
{
    /// <summary>
    ///  <para>
    ///   All properties of an organism that are used by the game are on this object.  Each
    ///   OrganismState is immutable and references can be held as long as the organism
    ///   needs them.
    ///  </para>
    ///  <para>
    ///   This object only represents the state of a creature for a given turn and is not
    ///   dynamically updated.  Creatures should use the LookFor method in order to
    ///   get the latest state of another creature.
    ///  </para>
    /// </summary>
    [Serializable]
    public abstract class OrganismState : IComparable
    {
        /// <summary>
        ///  The radius of the organism.  The actual size is twice the
        ///  radius.
        /// </summary>
        private int radius = 0;

        /// <summary>
        ///  A GUID representing the unique ID of this organism.
        /// </summary>
        private string organismID;

        /// <summary>
        ///  Represent the current movement action.  Should
        ///  be cleared during a teleport since the world
        ///  may be different along with available destinations
        ///  and paths.
        /// </summary>
        [NonSerialized]
        private MoveToAction currentMoveToAction;

        /// <summary>
        ///  Represents the current reproduction action.  Should
        ///  be cleared during a teleport since the new
        ///  situations after the teleportation might
        ///  change whether the creature wants to reproduce
        ///  or not.
        /// </summary>
        [NonSerialized]
        private ReproduceAction currentReproduceAction;

        /// <summary>
        ///  The number of ticks the creature has been incubating
        ///  a child.  This also gets reset during teleportation
        ///  since it is dependent on the currentReproduceAction.
        /// </summary>
        [NonSerialized]
        private int incubationTicks = 0;

        /// <summary>
        ///  The creature's current location in the world.  This
        ///  item is still serialized since it is important when
        ///  reloading a game state.
        /// </summary>
        private Point currentPosition;

        /// <summary>
        ///  Determines if the creature is alive or not.
        ///  Dead creatures should never be teleported, but
        ///  the variable is still serialized for game states.
        /// </summary>
        private Boolean isAlive = true;

        /// <summary>
        ///  The amount of energy the creature currently has.
        /// </summary>
        private double energy = 0;

        /// <summary>
        ///  A pointer to the species for this creature.  Each
        ///  creature can identify various basic traits by
        ///  it's species.
        /// </summary>
        private ISpecies species;

        /// <summary>
        ///  The amount of food chunks this creature represents.
        ///  This is important for plants and dead animals.
        /// </summary>
        /// <internal/>
        protected int currentFoodChunks = 0;

        /// <summary>
        ///  The current age of the creature in game ticks.
        /// </summary>
        private int tickAge = 0;

        /// <summary>
        ///  A generational number used to figure out how
        ///  long a species has been alive and how many
        ///  times reproduction has occured.
        /// </summary>
        private int generation = 0;

        /// <summary>
        ///  The amount of time in game ticks a creature
        ///  must wait before being capable of reproduction.
        /// </summary>
        private int reproductionWait = 0;

        /// <summary>
        ///  The amount of time in game ticks a creature
        ///  must wait before being capable of growing.
        /// </summary>
        private int growthWait = 0;

        /// <summary>
        ///  The direction the creature is moving in degrees.
        /// </summary>
        private int actualDirection = 0;

        /// <summary>
        ///  The creature's death reason.  Generally this is the
        ///  special value NotDead meaning the creature is still
        ///  alive.  It will be changed to another reason upon the
        ///  creature's death or teleportation.
        /// </summary>
        private PopulationChangeReason deathReason = PopulationChangeReason.NotDead;

        /// <summary>
        ///  <para>
        ///   This is a special object that is used to hold game
        ///   engine information.  Primarily used by the rendering engine
        ///   it contains information about selection, graphical sprite
        ///   information, animation offsets, and anything else that
        ///   needs to be specified by the gaming engine that shouldn't
        ///   be directly part of the OrganismState itself.
        ///  </para>
        ///  <para>
        ///   This object is not serialized because all animation information
        ///   needs to be reset after switching machines.
        ///  </para>
        /// </summary>
        /// <internal/>
        [NonSerialized]
        private object renderInfo;

        /// <summary>
        ///  The results of any actions are stored here.  During a creature's
        ///  tick these event results fire various events.
        /// </summary>
        /// <internal/>
        [NonSerialized]
        private OrganismEventResults events;

        /// <summary>
        ///  Determines if the state object shold be able to be modified
        ///  or whether the object should keep its current values regardless
        ///  of what methods are called or properties are changed.
        /// </summary>
        /// <internal/>
        bool immutable = false;

        /// <summary>
        ///  A less restrictive immutability flag used just for locking
        ///  location and size properties.
        /// </summary>
        // Once we've built the WorldStateindex, if size and position change it really screws things up 
        // and we don't have a mechanism to have the index get updated.  This allows us to lock them
        // down.
        private bool lockedSizeAndPosition = false;


        /// <summary>
        ///  Create a new state object to represent an organism.
        /// </summary>
        /// <param name="id">The GUID ID representing this organism in the world.</param>
        /// <param name="species">The species which defines the basic properties of the organism.</param>
        /// <param name="generation">The familial generation number.</param>
        internal OrganismState(string id, ISpecies species, int generation)
        {
            this.organismID = id;
            this.species = species;
            this.generation = generation;
            this.events = new OrganismEventResults();
        }

        /// <summary>
        ///  Performs a special type of immutability lock for the size
        ///  and position related properties only.  This is to ensure that
        ///  the area of a creature isn't changed after the creature's index
        ///  array position has been found in the game world.
        /// </summary>
        /// <internal/>
        public void LockSizeAndPosition()
        {
            if (immutable)
            {
                throw new GameEngineException("Object is immutable.");
            }

            lockedSizeAndPosition = true;
        }

        /// <summary>
        ///  Makes all properties immutable.  Ensures that the organism state
        ///  cannot be changed at all by creatures with access to the state.
        /// </summary>
        /// <internal/>
        public void MakeImmutable()
        {
            if (events != null)
            {
                events.MakeImmutable();
            }

            immutable = true;
        }

        /// <summary>
        ///  Determines if the current creature state is immutable.
        /// </summary>
        /// <internal/>
        public bool IsImmutable
        {
            get
            {
                return immutable;
            }
        }

        /// <summary>
        ///  Derived classes must override this to return an instance of their class type
        ///  that has the same state (by calling CopyStateInto)
        /// </summary>
        /// <internal/>
        public abstract OrganismState CloneMutable();

        /// <summary>
        ///  Derived classes must override (and call Base.CopyStateInto)
        ///  if they have additional state
        /// </summary>
        /// <param name="newInstance">The new state that will hold this state's members</param>
        /// <internal/>
        protected virtual void CopyStateInto(OrganismState newInstance)
        {
            // OrganismID and species are copied via the constructor
            newInstance.radius = radius;

            // Safe because they are immutable
            newInstance.currentMoveToAction = currentMoveToAction;
            newInstance.currentReproduceAction = currentReproduceAction;

            // Points aren't immutable, so return a copy
            newInstance.currentPosition = new Point(currentPosition.X, currentPosition.Y);

            newInstance.isAlive = isAlive;
            newInstance.energy = energy;
            newInstance.currentFoodChunks = currentFoodChunks;
            newInstance.incubationTicks = incubationTicks;
            newInstance.tickAge = tickAge;
            newInstance.reproductionWait = reproductionWait;
            newInstance.growthWait = growthWait;
            newInstance.deathReason = deathReason;
            newInstance.actualDirection = actualDirection;

            // This object remains mutable because it is information
            // that the renderer updates after the WorldVector has been
            // created.  Nothing besides the renderer should ever touch this
            newInstance.renderInfo = renderInfo;
        }

        /// <summary>
        ///  Returns the action that the organism was performing
        ///  between the last state and this one.  I.e. what ended on
        ///  this state.
        /// </summary>
        /// <internal/>
        public virtual DisplayAction PreviousDisplayAction
        {
            get
            {

                if (!this.IsAlive)
                {
                    return DisplayAction.Dead;
                }

                if (OrganismEvents != null && OrganismEvents.Teleported != null)
                {
                    return DisplayAction.Teleported;
                }
            
                if (OrganismEvents != null && OrganismEvents.AttackCompleted != null)
                {
                    return DisplayAction.Attacked;
                }
            
                if (OrganismEvents != null && OrganismEvents.EatCompleted != null)
                {
                    return DisplayAction.Ate;
                }
            
                if ((OrganismEvents != null && OrganismEvents.MoveCompleted != null) ||
                    this.IsStopped != true)
                {
                    return DisplayAction.Moved;
                }
            
                if (OrganismEvents != null && OrganismEvents.DefendCompleted != null)
                {
                    return DisplayAction.Defended;
                }
            
                if ((OrganismEvents != null && OrganismEvents.ReproduceCompleted != null) ||
                    this.IsIncubating == true)
                {
                    return DisplayAction.Reproduced;
                }

                return DisplayAction.NoAction;
            }
        }

        /// <summary>
        ///  This property remains mutable even though the rest of the object is immutable
        ///  because it is used by the renderer and maintains rendering state across
        ///  worldstate instances (like whether the organism is selected or not).
        /// </summary>
        /// <internal/>
        public object RenderInfo
        {
            get
            {
                return renderInfo;
            }

            set
            {
                renderInfo = value;
            }
        }

        /// <summary>
        ///  When events are completed the completion actions get listed into
        ///  a collection of completed events.  These are then used by the
        ///  processing functions to fire off events on the creature.
        /// </summary>
        /// <internal/>
        public OrganismEventResults OrganismEvents
        {
            get
            {
                return events;
            }

            set
            {
                if (immutable)
                {
                    throw new GameEngineException("Object is immutable.");
                }
                
                events = value;
            }
        }

        /// <summary>
        ///  <para>
        ///   Describes the characteristics of a creature through the
        ///   use of the ISpecies interface.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  ISpecies interface representing the characteristics of the creature.
        /// </returns>
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public ISpecies Species
        {
            get
            {
                return species;
            }
        }

        /// <summary>
        ///  <para>
        ///   Determines if the creature is mature by comparing the current Radius
        ///   to the Radius it will have when mature.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  True if the creature is mature, False otherwise.
        /// </returns>
        public Boolean IsMature
        {
            get
            {
                return Radius == species.MatureRadius;
            }
        }

        /// <summary>
        ///  <para>
        ///   Describes the reason why the creature died.  This is most often
        ///   OldAge, Starvation, Killed, or Sickness.  If the creature is not
        ///   dead yet then the value will be NotDead.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  PopulationChangeReason describing the reason for death.
        /// </returns>
        public PopulationChangeReason DeathReason
        {
            get
            {
                return deathReason;
            }
        }

        /// <summary>
        ///  <para>
        ///   Represents the age of a creature in game ticks.  Once
        ///   a creature reaches a TickAge identical to its LifeSpan
        ///   the creature will die from PopulationChangeReason.OldAge.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Int32 representing the age of the creature in game ticks.
        /// </returns>
        public int TickAge
        {
            get
            {
                return tickAge;
            }
        }


        /// <summary>
        ///  <para>
        ///   Generation will be 0 the first time a creature is introduced.
        ///   Each offspring of a creature will be labeled with its generation
        ///   plus one.  This helps define the longevity of a creature.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Int32 representing the generation number for this creature.
        /// </returns>
        public int Generation
        {
            get
            {
                return generation;
            }
        }

        /// <summary>
        ///  Adds a single tick to the creature's current age.  This is
        ///  also responsible for ticking down other counters like growth
        ///  and reproduction.  When ticks hits the LifeSpan the creature
        ///  dies, but when the other counters reach 0 the action becomes
        ///  available.
        /// </summary>
        /// <internal/>
        public virtual void AddTickToAge()
        {
            if (immutable)
            {
                throw new GameEngineException("Object is immutable.");
            }

            if (!IsAlive)
            {
                throw new ApplicationException("Dead organisms can't age.");
            }
    
            tickAge++;
            if (growthWait != 0)
            {
                growthWait--;
            }

            if (reproductionWait != 0)
            {
                reproductionWait--;
            }

            if (tickAge > Species.LifeSpan)
            {
                Kill(PopulationChangeReason.OldAge);
            }
        }

        /// <summary>
        ///  Used to obtain or modify the current action representing
        ///  the creature's reproduction status.  If this value is not
        ///  null then the creature is reproducing.
        /// </summary>
        /// <internal/>
        public ReproduceAction CurrentReproduceAction
        {
            get
            {
                return currentReproduceAction;
            }
        
            set
            {
                if (immutable)
                {
                    throw new GameEngineException("Object is immutable.");
                }

                if (!IsAlive)
                {
                    throw new GameEngineException("Dead organisms can't reproduce.");
                }
                
                currentReproduceAction = value;
            
                if (value == null)
                {
                    incubationTicks = 0;
                }
                else
                {
                    Debug.Assert(incubationTicks==0, "Organism should not be able to start reproduction while already incubating.");
                }
            }
        }

        /// <summary>
        ///  <para>
        ///   Determines if a creature is ready to reproduce based on the
        ///   elapsed time since its previous reproduction.  Use ReproductionWait
        ///   to determine exactly how long the creature has to go.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  True if the creature is ready to reproduce.  False otherwise.
        /// </returns>
        public Boolean ReadyToReproduce
        {
            get
            {
                return reproductionWait == 0;
            }
        }


        /// <summary>
        ///  <para>
        ///   Determines the number of ticks the creature must wait before
        ///   reproducing again.  If your creature just needs to know if it's
        ///   ready the ReadyToReproduce property can be used instead.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Int32 representing the time in ticks before the creature can reproduce.
        /// </returns>
        public int ReproductionWait
        {
            get
            {
                return reproductionWait;
            }
        }

        /// <summary>
        ///  Sets the current reproduction wait of the creature to
        ///  the wait time specified by the creature's species.  This
        ///  value will be set based on the creature's lifespan and
        ///  creature type (whether carnivore or herbivore).  This is
        ///  called by the engine after reproduction has completed.
        /// </summary>
        /// <internal/>
        public void ResetReproductionWait()
        {
            if (immutable)
            {
                throw new GameEngineException("Object is immutable.");
            }

            reproductionWait = Species.ReproductionWait;
        }

        /// <summary>
        ///  <para>
        ///   Determines if the creature is in the process of reproduction.
        ///   Use IncubationTicks to find out exactly how long the creature
        ///   has left to incubate.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  True if the creature is in the process of reproducing, False otherwise.
        /// </returns>
        public Boolean IsIncubating
        {
            get
            {
                return currentReproduceAction != null;
            }
        }

        /// <summary>
        ///  <para>
        ///   Determines the number of ticks the creature must wait before it
        ///   has finished reproducing.  If your creature just needs to know
        ///   if it's currently reproducing use the IsIncubating property.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Int32 representing the number of ticks left for incubation.
        /// </returns>
        public int IncubationTicks
        {
            get
            {
                return incubationTicks;
            }
        }

        /// <summary>
        ///  Add a single tick to the current incubation period.  Called
        ///  by the game engine each tick after the creature starts reproducing.
        ///  Once the amount of ticks hits the limit for the amount of time
        ///  required to incubate a child, the creature is born, and incubation
        ///  is no longer required.
        /// </summary>
        /// <internal/>
        public void AddIncubationTick()
        {
            if (immutable)
            {
                throw new GameEngineException("Object is immutable.");
            }

            incubationTicks++;
        }

        /// <summary>
        ///  <para>
        ///   Determines the amount of food value this creature represents.
        ///   This can be used to determine if attacking a creature will
        ///   be worth the effort.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Int32 representing the number of food chunks this creature represents.
        /// </returns>
        public int FoodChunks
        {
            get
            {
                return currentFoodChunks;
            }
            set
            {
                if (immutable)
                {
                    throw new GameEngineException("Object is immutable.");
                }

                if (value <= 0)
                {
                    throw new GameEngineException("If foodchunks <= 0 the organism should be removed from the world.");
                }

                currentFoodChunks = value;
            }
        }

        /// <summary>
        ///  <para>
        ///   Determines how much energy a creature has stored.  This is used to
        ///   compute the energy state of the creature.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Double representing the amount of energy this creature has stored.
        /// </returns>
        public double StoredEnergy
        {
            get
            {
                return energy;
            }

            set
            {
                if (immutable)
                {
                    throw new GameEngineException("Object is immutable.");
                }

                if (!IsAlive)
                {
                    throw new GameEngineException("Dead organisms can't change stored energy.");
                }

                if (value <= 0)
                {
                    Kill(PopulationChangeReason.Starved);
                    return;
                }

                if (value > (double) Radius * species.MaximumEnergyPerUnitRadius)
                {
                    value = species.MaximumEnergyPerUnitRadius * (double) Radius;
                }

                energy = value;
            }
        }

        /// <summary>
        ///  Used by the game engine to burn a creature's energy depending
        ///  on the various actions they perform including movement, reproduction,
        ///  and growth.
        /// </summary>
        /// <internal/>
        public void BurnEnergy(double energy)
        {
            if (immutable)
            {
                throw new GameEngineException("Object is immutable.");
            }

            if (!IsAlive)
            {
                throw new GameEngineException("Dead organisms can't change stored energy.");
            }
        
            if (StoredEnergy - energy <= 0)
            {
                Kill(PopulationChangeReason.Starved);
            }
            else
            {
                StoredEnergy = StoredEnergy - energy;
            }
        }


        /// <summary>
        ///  <para>
        ///   Returns the amount of energy required to be at the top of a given EnergyState.
        ///   It's recommended that the actual EnergyState property be used to determine
        ///   the current energy bucket a creature is in.
        ///  </para>
        /// </summary>
        /// <param name="energyState">
        ///  EnergyState enum value for the bucket to get the upper energy bounding for.
        /// </param>
        /// <returns>
        ///  System.Double representing the amount of energy to be at the top of a given energy state.
        /// </returns>
        public double UpperBoundaryForEnergyState(EnergyState energyState)
        {
            double energyBuckets = (species.MaximumEnergyPerUnitRadius * (double) Radius) / (double) 5;

            switch (energyState)
            {
                case EnergyState.Dead:
                    return 0;
                case EnergyState.Deterioration:
                    return energyBuckets * (double) 1;
                case EnergyState.Hungry:
                    return energyBuckets * (double) 2;
                case EnergyState.Normal:
                    return energyBuckets * (double) 4;
                case EnergyState.Full:
                    return species.MaximumEnergyPerUnitRadius * (double) Radius;
                default:
                    throw new ApplicationException("Unknown EnergyState.");
            }
        }

        /// <summary>
        ///  <para>
        ///   Determine the current energy state of a creature by comparing
        ///   the current amount of stored energy versus the various energy
        ///   buckets.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  EnergyState enum representing the creature's current energy state.
        /// </returns>
        public EnergyState EnergyState
        {
            get
            {
                int energyBuckets = (species.MaximumEnergyPerUnitRadius * Radius) / 5;

                if (energy > energyBuckets * 4)
                {
                    return EnergyState.Full;
                }
                else if (energy > energyBuckets * 2)
                {
                    return EnergyState.Normal;
                }
                else if (energy > energyBuckets * 1)
                {
                    return EnergyState.Hungry;
                }
                else if (energy > 0)
                {
                    return EnergyState.Deterioration;
                }
                else
                {
                    return EnergyState.Dead;
                }
            }
        }

        /// <summary>
        ///  <para>
        ///   Determines the decimal percentage of the amount of energy
        ///   a creature currently has, versus the total amount of energy
        ///   the creature can store.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Double between 0 and 1, with 0 being none and 1 being maxed.
        /// </returns>
        public double PercentEnergy
        {
            get
            {
                Debug.Assert((((double) energy / (double) (Species.MaximumEnergyPerUnitRadius * Radius)) * (double) 100)
                    <= 100);

                return (((double) energy / (double) (Species.MaximumEnergyPerUnitRadius * Radius)));
            }
        }

        /// <summary>
        ///  <para>
        ///   Determines the decimal percentage of the amount of life remaining
        ///   a creature currently has, versus the total amount of LifeSpan.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Double between 0 and 1, with 1 being newly born, and 0 being dead.
        /// </returns>
        public double PercentLifespanRemaining
        {
            get
            {
                Debug.Assert(1 - (((double) this.TickAge / (double) (Species.LifeSpan)) * (double) 100)
                    <= 100);
                return ((double) 1 - ((double) this.TickAge / (double) (Species.LifeSpan)));
            }
        }

        /// <summary>
        ///  Must be overriden in derived classes and compute a factor that
        ///  can be used to represent the injury done to the creature.
        /// </summary>
        /// <internal />
        public abstract double PercentInjured
        {
            get;
        }

        /// <summary>
        ///  <para>
        ///   Returns the creature's current position as a Point.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Drawing.Point representing the creature's current location.
        /// </returns>
        public Point Position
        {
            // Points aren't immutable, so return a copy
            get
            {
                return new Point(currentPosition.X, currentPosition.Y);
            }
        
            set
            {
                if (immutable)
                {
                    throw new GameEngineException("Object is immutable.");
                }

                if (lockedSizeAndPosition)
                {
                    throw new GameEngineException("Objects position and size are locked.");
                }

                if (!IsAlive)
                {
                    throw new GameEngineException("Dead organisms can't move.");
                }

                currentPosition.X = value.X;
                currentPosition.Y = value.Y;
                SetBitmapDirection();
            }
        }

        /// <summary>
        ///  <para>
        ///   Retrieves the creature's current grid location.  This is useful for
        ///   movement algorithms and is used by the engine for computing organism
        ///   location in a fast and memory efficient manner.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Int32 representing the Terrarium grid cell for the center of the organism.
        /// </returns>
        public int GridX
        {
            get
            {
                return Position.X >> EngineSettings.GridWidthPowerOfTwo;
            }
        }

        /// <summary>
        ///  <para>
        ///   Retrieves the creature's current grid location.  This is useful for
        ///   movement algorithms and is used by the engine for computing organism
        ///   location in a fast and memory efficient manner.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Int32 representing the Terrarium grid cell for the center of the organism.
        /// </returns>
        public int GridY
        {
            get
            {
                return Position.Y >> EngineSettings.GridHeightPowerOfTwo;
            }
        }

        /// <summary>
        ///  <para>
        ///   Determines the number of game cells a creature uses on the
        ///   screen.  Each grid cell is 8 pixels in width and height.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Int32 for the grid cells the creature's radius represents.
        /// </returns>
        public int CellRadius 
        {
            get
            {
                if (Radius % EngineSettings.GridCellWidth > 0)
                {
                    return (Radius >> EngineSettings.GridWidthPowerOfTwo) + 1;
                }
                else
                {
                    return Radius >> EngineSettings.GridWidthPowerOfTwo;
                }
            }
        }

        /// <summary>
        ///  <para>
        ///   Determines the actual radius of the creature.  This is used to
        ///   determine how close to maturity a creature has gotten or in
        ///   various computations on attack, defense, and movement.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Int32 of the actual radius of the creature.
        /// </returns>
        public int Radius
        {
            get
            {
                return radius;
            }
        }

        /// <summary>
        ///  Used by the game engine to increase the radius of a creature to a new radius
        ///  amount.  Each time a creature grows this method is called to set the new radius.
        ///  The new radius must be larger than the previous one, so creatures can't shrink.
        ///  The radius can be increased by more than a single unit, but the current
        ///  Terrarium only encompasses methods that increase a radius by a single unit.
        /// </summary>
        /// <internal/>
        public virtual void IncreaseRadiusTo(int newRadius)
        {
            if (immutable)
            {
                throw new GameEngineException("Object is immutable.");
            }

            if (lockedSizeAndPosition)
            {
                throw new GameEngineException("Objects position and size are locked.");
            }

            if (!IsAlive)
            {
                throw new GameEngineException("Dead organisms can't grow.");
            }

            if (newRadius <= Radius)
            {
                throw new GameEngineException("New radius must be bigger than old one.");
            }

            radius = newRadius;
        }

        /// <summary>
        ///  <para>
        ///   A string number in the form of a GUID that uniquely represents
        ///   this creature in the EcoSystem.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.String uniquely identifying this creature in the EcoSystem.
        /// </returns>
        public string ID
        {
            get
            {
                return organismID;
            }
        }

        /// <summary>
        ///  Retrieves information about the creature's current movement vector.
        ///  If the creature is moving this value will always be non null, and
        ///  null if the creature isn't currently moving.  This is used by the
        ///  game engine since movement can encompass many turns.
        /// </summary>
        /// <internal/>
        public MoveToAction CurrentMoveToAction
        {
            get
            {
                return currentMoveToAction;
            }

            set
            {
                if (immutable)
                {
                    throw new GameEngineException("Object is immutable.");
                }

                if (!IsAlive)
                {
                    throw new GameEngineException("Dead organisms can't move.");
                }

                currentMoveToAction = value;
                SetBitmapDirection();
            }
        }

        /// <summary>
        ///  <para>
        ///   Determines the speed at which the creature is moving.  Useful
        ///   in calculating overtake speeds for Carnivores and run-away
        ///   speeds for Herbivores.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Int32 for the speed the creature is moving.
        /// </returns>
        public int Speed
        {
            get
            {
                if (currentMoveToAction != null)
                {
                    return currentMoveToAction.MovementVector.Speed;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        ///  <para>
        ///   Determines the direction the creature is moving in degrees.
        ///   This along with Speed can be used to calculate where a creature
        ///   will be in the future.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Int32 representing the direction the creature is moving in degrees.
        /// </returns>
        public int ActualDirection
        {
            get 
            {
                return actualDirection;
            }
        }

        /// <summary>
        ///  <para>
        ///   Determines if the creature is moving or is completely stopped.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  True if the creature is moving, False otherwise.
        /// </returns>
        public Boolean IsStopped
        {
            get
            {
                return currentMoveToAction == null;
            }
        }

        /// <summary>
        ///  Determines the facing of the creature based on the movement
        ///  vector and update the creature's actual direction as a result.
        /// </summary>
        private void SetBitmapDirection()
        {
            if (immutable)
            {
                throw new GameEngineException("Object is immutable.");
            }

            if (CurrentMoveToAction != null)
            {
                Vector direction = Vector.Subtract(CurrentMoveToAction.MovementVector.Destination,
                    currentPosition);
                Vector unitVector = direction.GetUnitVector();

                double angle = Math.Acos(unitVector.X);
                if (unitVector.Y < 0)
                {
                    angle = 6.2831853 - angle;
                }

                // convert radians to degrees
                actualDirection = (int) ((angle / 6.283185) * 360);
            }
        }

        /// <summary>
        ///  <para>
        ///   Determines if the creature is alive or dead.  This is used by
        ///   Carnivores so they can find food in the form of corpses.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  True if the creature is alive, False otherwise.
        /// </returns>
        public Boolean IsAlive
        {
            get
            {
                return isAlive;
            }
        }

        /// <summary>
        ///  Called by the game engine in order to kill the current creature.
        ///  Since this method can only be called when the state is mutable
        ///  player's can't use the method to arbitrarily kill competing
        ///  organisms.
        /// </summary>
        /// <internal/>
        public void Kill(PopulationChangeReason reason)
        {
            if (immutable)
            {
                throw new GameEngineException("Object is immutable.");
            }
        
            isAlive = false;
            currentMoveToAction = null;
            energy = 0;
            deathReason = reason;
        }

        /// <summary>
        ///  This returns a clone because we need the radius to be editable
        ///  and the object may not allow this because it is already in the index
        ///  the object returned will have the same events as the original
        /// </summary>
        /// <internal/>
        public abstract OrganismState Grow();

        /// <summary>
        ///  <para>
        ///   Determines the amount of time in game ticks a creature must wait
        ///   before they are able to grow.  If a creature is not yet mature,
        ///   and the GrowthWait is 0, then it is possible the creature does
        ///   not have enough energy or enough space in order to grow.  This
        ///   should be remedied quickly.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Int32 for the amount of time in game ticks a creature has before they can grow.
        /// </returns>
        public int GrowthWait
        {
            get
            {
                return growthWait;
            }
        }

        /// <summary>
        ///  Used by the game engine to reset the amount of time the creature
        ///  must wait before growing.  The wait time is based on the creature's
        ///  species class which uses the creature's lifepsan as a base for the
        ///  growth period.  This should be called after a creature has been
        ///  given the chance to grow, and must wait before growing again.
        /// </summary>
        /// <internal/>
        public void ResetGrowthWait()
        {
            if (immutable)
            {
                throw new GameEngineException("Object is immutable.");
            }
        
            growthWait = Species.GrowthWait;
        }

        /// <summary>
        ///  Required override for other state classes.  This method gives
        ///  the creature a chance to heal some previously inflicted damage.
        /// </summary>
        /// <internal/>
        public abstract void HealDamage();

        /// <summary>
        ///  <para>
        ///   Determines if a creature is immediately next to or overlapping
        ///   another creature using grid cells comparisons.
        ///  </para>
        /// </summary>
        /// <param name="state2">
        ///  OrganismState of the creature to check for proximity.
        /// </param>
        /// <returns>
        ///  True if the creature is adjacent or overlapping, False otherwise.
        /// </returns>
        public Boolean IsAdjacentOrOverlapping(OrganismState state2)
        {
            return IsWithinRect(0, state2);
        }

        /// <summary>
        ///  Used to compute whether or not a given state object is in an adjacent or
        ///  overlapping grid cell.  The extra radius can be used to extend the area
        ///  used for the function to find a match and so can be used for functions
        ///  like visibiliy.
        /// </summary>
        /// <param name="state1ExtraRadius">The amount of extra grid cells to add</param>
        /// <param name="state2">The organism state of the creature to use in the area test.</param>
        /// <returns>True if the creature is within range, false otherwise.</returns>
        public Boolean IsWithinRect(int state1ExtraRadius, OrganismState state2)
        {
            if (null == state2)
            {
                return false;
            }

            int state1Radius = this.CellRadius + state1ExtraRadius;
            int state2Radius = state2.CellRadius;

            int difference = (this.GridX - state1Radius) - (state2.GridX - state2Radius);
            if (difference < 0)
            {
                // Negative means state1 boundary < state2 boundary
                if (-difference > (state1Radius * 2) + 1)
                {
                    // X isn't overlapping or adjacent
                    return false;
                }
            }
            else
            {
                // state2 boundary <=  state1 boundary
                if (difference > (state2Radius * 2) + 1)
                {
                    // X isn't overlapping or adjacent
                    return false;
                }
            }

            difference = (this.GridY - state1Radius) - (state2.GridY - state2Radius);
            if (difference < 0)
            {
                // Negative means state1 boundary < state2 boundary
                if (-difference > (state1Radius * 2) + 1)
                {
                    // Y isn't overlapping or adjacent
                    return false;
                }
            }
            else
            {
                // state2 boundary <=  state1 boundary
                if (difference > (state2Radius * 2) + 1)
                {
                    // Y isn't overlapping or adjacent
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///  Compares two organism state objects together.  This method takes into
        ///  account the Y position for graphical Z-Ordering purposes and can be used
        ///  to sort creatures for back to front rendering.
        /// </summary>
        /// <param name="b">The object to be compared.  Has to be another OrganismState.</param>
        /// <returns>Less than 0 if the zOrder is less, 0 for equal, and greater than 0 for more.</returns>
        /// <internal/>
        public int CompareTo(Object b)
        {
            if (b is OrganismState)
            {
                return currentPosition.Y.CompareTo(((OrganismState) b).Position.Y);
            }
            else
            {
                return 0;
            }
        }
    }
}