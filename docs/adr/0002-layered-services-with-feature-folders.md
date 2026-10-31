# ADR 0002: Layers ανά service με οργάνωση ανά λειτουργία

* **Κατάσταση:** Αποδεκτή
* **Ημερομηνία:** 2026-10-31

## Πλαίσιο

Κάθε domain service (Members, Classes, Court, Academy) χρειάζεται εσωτερική οργάνωση του κώδικα. Η αρχική επιλογή (5 Οκτωβρίου 2026) ήταν διάσπαση σε layers. Αναθεωρήθηκε στις 31 Οκτωβρίου 2026, όταν εξετάστηκε η σχέση της με το WolverineFx.

Δύο απαιτήσεις επηρεάζουν την απόφαση:

* Ο ερευνητικός πυρήνας ζητά δύο στρατηγικές ελέγχου ταυτοχρονισμού (optimistic και pessimistic) **πίσω από κοινό interface**, ώστε να συγκριθούν πειραματικά χωρίς αλλαγή στη λογική των εγγραφών.
* Οι επιχειρησιακοί κανόνες (χωρητικότητα, ηλικιακοί περιορισμοί, επικαλύψεις) πρέπει να ελέγχονται με unit tests χωρίς βάση δεδομένων.

Ταυτόχρονα, το WolverineFx σχεδιάστηκε με vertical slices κατά νου: τα handlers του δουλεύουν πιο φυσικά όταν δέχονται απευθείας τις εξαρτήσεις τους, και ο δημιουργός του τάσσεται κατά των πολλών layers.

## Απόφαση

Κάθε service αποτελείται από τέσσερα projects, με εξαρτήσεις μόνο προς τα μέσα:

* **Domain:** entities και επιχειρησιακοί κανόνες. Δεν εξαρτάται από κανένα άλλο project ή framework.
* **Application:** use cases. Εξαρτάται από το Domain και από το πακέτο του EF Core (όχι από τον PostgreSQL provider, βλ. «Πρόσβαση σε δεδομένα»).
* **Infrastructure:** EF Core (Entity Framework Core), PostgreSQL, WolverineFx. Υλοποιεί τα interfaces του Application.
* **Api:** endpoints και σύνθεση των παραπάνω.

Μέσα στο Application ο κώδικας οργανώνεται **ανά λειτουργία** και όχι ανά τεχνικό ρόλο:

```
Evolve.Classes.Application/
  Enrollments/
    EnrollInClass/
    CancelEnrollment/
  Waitlist/
    PromoteFromWaitlist/
```

αντί για φακέλους όπως `Commands/`, `Handlers/`, `Validators/`.

Ο Domain κρατά πραγματικά τους κανόνες: για παράδειγμα, η entity του τμήματος αρνείται εγγραφή πάνω από τη χωρητικότητα. Τα handlers συντονίζουν, δεν αποφασίζουν.

### Πρόσβαση σε δεδομένα

Το Application ορίζει ένα interface για το DbContext κάθε service (π.χ. `IClassesDbContext`) με τα `DbSet` και το `SaveChangesAsync`. Το Infrastructure το υλοποιεί με το πραγματικό DbContext. Τα handlers χρησιμοποιούν απευθείας LINQ πάνω στα `DbSet`, χωρίς repositories. Το Application εξαρτάται από το πακέτο του EF Core, όχι από τον PostgreSQL provider. Το Domain μένει χωρίς καμία εξάρτηση από το EF Core.

Αιτιολόγηση: το `DbSet` υλοποιεί ήδη το Repository pattern και το `DbContext` το Unit of Work. Ένα repository από πάνω θα ήταν αφαίρεση πάνω σε αφαίρεση. Τα handlers ελέγχονται ούτως ή άλλως με πραγματική PostgreSQL, αφού ο ερευνητικός πυρήνας βασίζεται σε μηχανισμούς της βάσης (exclusion constraint, `SELECT FOR UPDATE`).

**Προϋπόθεση:** η απόφαση ισχύει υπό την επιφύλαξη επαλήθευσης στο walking skeleton. Η πολιτική `AutoApplyTransactions` του WolverineFx ενεργοποιεί τη transactional middleware (και μαζί την εγγραφή στο outbox μέσα στο ίδιο transaction) μόνο όταν το handler έχει εξάρτηση από `DbContext`. Ένα handler που δέχεται `IClassesDbContext` ενδέχεται να μην αναγνωρίζεται. Η επαλήθευση: ένα handler του Members με `IMembersDbContext`, έλεγχος του κώδικα που παράγει το WolverineFx για το handler. Αν δεν εφαρμόζεται transaction, δοκιμάζεται ρητή δήλωση (π.χ. `[Transactional]`). Μόνο αν αποτύχει και αυτό, η απόφαση επανεξετάζεται.

## Εναλλακτικές που εξετάστηκαν

* **Vertical slices σε ένα project ανά service:** απορρίφθηκε. Είναι το πιο φυσικό μοντέλο για το WolverineFx και έχει τον λιγότερο κώδικα. Όμως τα όρια ανάμεσα σε domain και υποδομή θα τα φύλαγε μόνο η πειθαρχία του developer, ενώ με ξεχωριστά projects τα φυλάει ο compiler: το Domain δεν έχει reference στο EF Core και δεν μπορεί να το χρησιμοποιήσει κατά λάθος. Επιπλέον, η καθαρή διάκριση των layers κάνει την αρχιτεκτονική άμεσα αναγνωρίσιμη.
* **Repository interfaces ανά aggregate στο Application:** απορρίφθηκε. Κρατά το Application εντελώς ανεξάρτητο από το EF Core, όμως απαιτεί ένα interface και μια υλοποίηση για κάθε aggregate που συνήθως απλώς προωθούν την κλήση στο EF Core. Προστατεύει από αλλαγή ORM (Object Relational Mapper), που δεν αναμένεται. Επιπλέον, έχει το ίδιο πρόβλημα με την επιλεγμένη λύση στην αυτόματη ανίχνευση transaction του WolverineFx, άρα δεν το λύνει.
* **Layers με οργάνωση ανά τεχνικό ρόλο** (`Commands/`, `Handlers/`): απορρίφθηκε. Μια αλλαγή σε μία λειτουργία απαιτεί άνοιγμα πολλών φακέλων μέσα στο ίδιο project, χωρίς κανένα όφελος.

## Συνέπειες

* **Θετικές:** τα όρια μεταξύ domain και υποδομής επιβάλλονται από τον compiler. Οι δύο στρατηγικές ταυτοχρονισμού έχουν φυσική θέση (interface στο Application, υλοποιήσεις στο Infrastructure). Το Domain ελέγχεται με unit tests χωρίς βάση. Η λογική κάθε λειτουργίας βρίσκεται σε έναν φάκελο.
* **Αρνητικές:** 16 projects για τα τέσσερα services και περισσότερος κώδικας σύνδεσης (interfaces, mappings). Η τοπικότητα είναι **μερική**: μια λειτουργία εξακολουθεί να αγγίζει τρία projects (endpoint στο Api, handler στο Application, ρυθμίσεις στο Infrastructure). Η απόφαση δεν δίνει τα πλήρη οφέλη των vertical slices, μόνο συγκεντρώνει τη λογική κάθε λειτουργίας. Τέλος, η χρήση του WolverineFx πηγαίνει εν μέρει κόντρα στο προτεινόμενο μοντέλο του.
* **Κίνδυνος:** τα layers δεν εγγυώνται καλό domain. Είναι δυνατόν να υπάρχει τέλειος διαχωρισμός projects με entities που είναι απλές συλλογές properties και όλους τους κανόνες στα handlers (anemic domain model). Ο κανόνας «ο Domain κρατά τους κανόνες» ελέγχεται στο code review και με unit tests στο Domain.
* **Επαλήθευση:** η προϋπόθεση της ενότητας «Πρόσβαση σε δεδομένα» ελέγχεται στο walking skeleton και το αποτέλεσμα καταγράφεται σε αυτό το ADR. Αν η απόφαση αλλάξει, γράφεται νέο ADR που αντικαθιστά το σχετικό τμήμα.
