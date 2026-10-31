# ADR 0006: xUnit v3 με Microsoft Testing Platform v2

* **Κατάσταση:** Αποδεκτή
* **Ημερομηνία:** 2026-10-31

## Πλαίσιο

Το Evolve χρειάζεται test framework για τρία είδη tests: unit tests του Domain και του Application, integration tests με πραγματική PostgreSQL και RabbitMQ, και τα πειράματα της σύγκρισης ελέγχου ταυτοχρονισμού (optimistic έναντι pessimistic), που στέλνουν ταυτόχρονα αιτήματα.

Στο .NET υπάρχουν πλέον δύο πλατφόρμες εκτέλεσης tests. Το VSTest είναι ο παλιός μηχανισμός πίσω από το `dotnet test` και τους Test Explorers. Το MTP (Microsoft Testing Platform) είναι ο διάδοχός του: κάθε test project γίνεται αυτόνομο εκτελέσιμο, χωρίς ξεχωριστό host. Το .NET 10 SDK υποστηρίζει εγγενώς το MTP στο `dotnet test`, εφόσον αυτό δηλωθεί στο `global.json`. Η επιλογή αφορά επομένως δύο άξονες: το framework (πώς γράφονται τα tests) και την πλατφόρμα (πώς εκτελούνται).

## Απόφαση

* **Framework:** xUnit v3, μέσω του πακέτου `xunit.v3.mtp-v2` (έκδοση στο `Directory.Packages.props`, ADR 0005).
* **Πλατφόρμα:** MTP v2. Το `global.json` (ADR 0003) περιέχει `"test": { "runner": "Microsoft.Testing.Platform" }`, ώστε το `dotnet test` του .NET 10 SDK να τρέχει σε λειτουργία MTP.
* **Test projects:** `OutputType` σε `Exe`, χωρίς τα πακέτα `Microsoft.NET.Test.Sdk` και `xunit.runner.visualstudio`, αφού το VSTest δεν χρησιμοποιείται. Ζουν στο `tests/` ανά service (ADR 0001) και κληρονομούν τις ρυθμίσεις του `tests/Directory.Build.props` (ADR 0004).

## Εναλλακτικές που εξετάστηκαν

* **NUnit:** απορρίφθηκε. Ώριμο framework που υποστηρίζει πλέον και MTP. Το xUnit προτιμήθηκε γιατί δημιουργεί νέο instance της κλάσης για κάθε test και δεν έχει μηχανισμό τύπου `[SetUp]` με κοινή κατάσταση, κάτι που ενθαρρύνει την απομόνωση των tests. Είναι επίσης το framework των περισσότερων παραδειγμάτων για `WebApplicationFactory` και Testcontainers, που θα χρειαστούν στα integration tests.
* **MSTest:** απορρίφθηκε. Είναι το framework της Microsoft με την πιο άμεση υποστήριξη MTP, όμως έχει μικρότερη παρουσία σε open source projects και παραδείγματα integration testing. Η επιλογή δεν οφείλεται σε τεχνική αδυναμία του.
* **TUnit:** απορρίφθηκε. Σχεδιασμένο εξαρχής για MTP, με source generation και παράλληλη εκτέλεση by default. Είναι όμως νεότερο, με μικρότερο οικοσύστημα, και η παράλληλη εκτέλεση by default απαιτεί προσοχή στα integration tests που μοιράζονται βάση δεδομένων. Η υποστήριξη coverage του dotCover για TUnit εμφανίστηκε μόλις στο Rider 2026.3 EAP. Για μια διπλωματική, ένα καθιερωμένο framework είναι ευκολότερο να τεκμηριωθεί.
* **xUnit v3 με VSTest ή MTP v1 (πακέτο `xunit.v3`):** απορρίφθηκε. Το MTP v1 είναι ακόμη το default του xUnit v3 και συνεργάζεται με το VSTest, όμως το MTP v2 είναι η κατεύθυνση του .NET 10 SDK. Με MTP v2 και .NET 10 SDK το VSTest δεν υποστηρίζεται, οπότε η επιλογή είναι οριστική και όχι μεικτή.
* **xUnit v2:** απορρίφθηκε. Βρίσκεται σε maintenance mode, χωρίς νέες λειτουργίες.

## Συνέπειες

* **Θετικές:** τα test projects είναι αυτόνομα εκτελέσιμα και μπορούν να τρέξουν και με `dotnet run`. Η εκτέλεση ακολουθεί την πλατφόρμα που προωθεί το ίδιο το SDK, οπότε δεν θα χρειαστεί μετάβαση αργότερα.
* **Αρνητικές:** εργαλεία που υποστηρίζουν μόνο VSTest δεν λειτουργούν. Η σύνταξη των επιλογών του `dotnet test` διαφέρει από το VSTest (π.χ. τα φίλτρα και οι αναφορές TRX, Test Results XML), οπότε οδηγίες γραμμένες για VSTest δεν ισχύουν αυτούσιες. Το CI πρέπει να χρησιμοποιεί το `global.json` του repo για να τρέξει σε λειτουργία MTP.
* **Περιορισμός IDE (RIDER-129745):** ο Rider δεν εντόπιζε tests MTP v2 στις εκδόσεις 2025.2, 2025.3 και 2026.1.1. Το workaround ήταν η προσθήκη του `Microsoft.NET.Test.Sdk`, που επαναφέρει το VSTest και αναιρεί την απόφαση. Το issue διορθώθηκε στο Rider 2026.2 EAP 6, οπότε το Evolve απαιτεί **Rider 2026.2 ή νεότερο**. Το workaround δεν εφαρμόστηκε.
* **Επαλήθευση:** στις 31/10/2026 ο Rider 2026.2.4 εντόπισε το `SmokeTests` του `Evolve.Members.Domain.Tests` χωρίς το `Microsoft.NET.Test.Sdk`. Την ίδια μέρα το `dotnet test` στη ρίζα του repo έτρεξε σε λειτουργία MTP, διαβάζοντας το `global.json`, και το test πέρασε (total: 1, succeeded: 1). Το προσωρινό smoke test αφαιρείται με το πρώτο πραγματικό test του Domain.
