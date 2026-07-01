using System;
using System.Collections.Generic;
using TimeTracker.Models;

Random random = new(260630);
TimeTrackerModel model = TimeTrackerModel.Instance;
DateTime today = DateTime.Today;
DateTime firstSeedMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-2);

ClientSeed[] clients =
{
  new("Aster Analytics", "Aster", "AA/JN", 56, 85m, "£", new[] { "Data Review", "Reporting" }),
  new("Bramble Works", "Bramble", "BW/JN", 104, 72m, "£", new[] { "Website Care" }),
  new("Copperline Studio", "Copperline", "CS/JN", 18, 95m, "$", new[] { "Product Design", "Prototype", "Launch Support" }),
  new("Driftmark Legal", "Driftmark", "DL/JN", 203, 110m, "£", new[] { "Case Admin", "Document Automation" }),
  new("Elm & Finch", "Elm Finch", "EF/JN", 39, 68m, "€", new[] { "Operations", "Monthly Support" }),
  new("Northstar Labs", "Northstar", "NL/JN", 7, 120m, "£", new[] { "Research", "Integration", "QA" })
};

int clientsCreated = 0;
int projectsCreated = 0;
int jobsCreated = 0;

foreach (ClientSeed clientSeed in clients)
{
  Client client = model.CreateClient($"{clientSeed.Name} Test");
  model.UpdateClient(
    client,
    $"{clientSeed.Name} Test",
    $"{clientsCreated + 10} Example Street, Test City",
    clientSeed.CompanyName,
    $"accounts@{clientSeed.CompanyName.Replace(" ", string.Empty).ToLowerInvariant()}.example",
    $"+44 20 7946 {1000 + clientsCreated}",
    clientSeed.InvoicePrefix,
    clientSeed.CurrentInvoiceNumber,
    clientSeed.DefaultHourlyRate,
    clientSeed.DefaultCurrency);
  clientsCreated++;

  List<Project> projects = new();
  foreach (string projectName in clientSeed.ProjectNames)
  {
    Project project = model.CreateProject(client, projectName, $"Seeded test project for {clientSeed.Name}.");
    model.UpdateProject(project, client, projectName, project.Description, Convert.ToDouble(clientSeed.DefaultHourlyRate));
    projects.Add(project);
    projectsCreated++;
  }

  for (int monthOffset = 0; monthOffset < 3; monthOffset++)
  {
    DateTime month = firstSeedMonth.AddMonths(monthOffset);
    int maxDay = month.Year == today.Year && month.Month == today.Month
      ? today.Day
      : DateTime.DaysInMonth(month.Year, month.Month);

    for (int jobIndex = 0; jobIndex < 15; jobIndex++)
    {
      Project project = projects[random.Next(projects.Count)];
      int day = Math.Min(maxDay, 1 + ((jobIndex * 2 + random.Next(0, 3)) % maxDay));
      int hour = random.Next(8, 17);
      int minute = random.Next(0, 4) * 15;
      DateTime start = new(month.Year, month.Month, day, hour, minute, 0);
      TimeSpan duration = TimeSpan.FromMinutes(random.Next(3, 19) * 15);
      string description = Descriptions[random.Next(Descriptions.Length)];

      model.CreateWorkEntry(
        project,
        start,
        description,
        clientSeed.DefaultHourlyRate,
        clientSeed.DefaultCurrency,
        duration);
      jobsCreated++;
    }
  }
}

Console.WriteLine($"Created {clientsCreated} clients, {projectsCreated} projects, and {jobsCreated} jobs.");
Console.WriteLine($"Months covered: {firstSeedMonth:MMMM yyyy}, {firstSeedMonth.AddMonths(1):MMMM yyyy}, {firstSeedMonth.AddMonths(2):MMMM yyyy}.");

internal sealed record ClientSeed(
  string Name,
  string CompanyName,
  string InvoicePrefix,
  int CurrentInvoiceNumber,
  decimal DefaultHourlyRate,
  string DefaultCurrency,
  string[] ProjectNames);

internal static partial class Program
{
  private static readonly string[] Descriptions =
  {
    "Planning session",
    "Implementation work",
    "Client review",
    "Bug fixing",
    "Documentation",
    "Research",
    "Data cleanup",
    "Report preparation",
    "Integration testing",
    "Follow-up actions"
  };
}
