namespace GTNSimulator.Simulation;

/// <summary>
///     Kontext zmeny jazdy - nastavenia simulácie a aktuálny virtuálny čas.
/// </summary>
/// <param name="Options">Nastavenia simulácie.</param>
/// <param name="Now">Aktuálny virtuálny čas. Dodatočné správy po zmene dostanú tento čas.</param>
public readonly record struct PlanEditContext(SimulationOptions Options, DateTime Now);
