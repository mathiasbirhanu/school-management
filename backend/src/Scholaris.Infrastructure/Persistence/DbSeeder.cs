using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Scholaris.Domain.Entities;
using Scholaris.Domain.Enums;
using Scholaris.Infrastructure.Identity;

namespace Scholaris.Infrastructure.Persistence;

/// <summary>
/// First-run seed: staff/student accounts, platform settings (22% commission default),
/// certificate template, demo catalog across K-12 and College/University (two courses carry
/// admin-set discounts to demo the balloon + price tags), and a few historical paid orders
/// so the revenue dashboards and CSV export show real-looking data immediately.
/// </summary>
public static partial class DbSeeder
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync()) return;

        var hasher = new PasswordHasher();

        var admin = new User { FullName = "System Admin", Email = "admin@scholaris.et", Role = UserRole.Admin, PasswordHash = hasher.Hash("Admin@123") };
        var sara = new User { FullName = "Sara Tesfaye", Email = "sara.tesfaye@scholaris.et", Role = UserRole.Teacher, PasswordHash = hasher.Hash("Teacher@123") };
        var daniel = new User { FullName = "Daniel Bekele", Email = "daniel.bekele@scholaris.et", Role = UserRole.Teacher, PasswordHash = hasher.Hash("Teacher@123") };
        var hanna = new User { FullName = "Hanna Girma", Email = "hanna.girma@scholaris.et", Role = UserRole.Teacher, PasswordHash = hasher.Hash("Teacher@123") };
        var abi = new User { FullName = "Abi Alemayehu", Email = "abi@student.scholaris.et", Role = UserRole.Student, PasswordHash = hasher.Hash("Student@123") };
        var selam = new User { FullName = "Selam Kebede", Email = "selam@student.scholaris.et", Role = UserRole.Student, PasswordHash = hasher.Hash("Student@123") };

        db.PlatformSettings.Add(new PlatformSetting());          // CommissionRatePercent = 22 (admin-adjustable)
        db.CertificateTemplates.Add(new CertificateTemplate());  // wording/design editable in the admin back office

        // ── Demo catalog ──────────────────────────────────────────────────────────────
        var math = C("Mathematics Mastery", "Mathematics", "Grade 8", 450m, 20m, sara, 258,
            "Fractions, ratios and first equations — the complete Grade 8 toolkit explained step by step with worked examples.");
        M(math, "Number Sense", 1,
            T("Fractions & Decimals", "fraction, decimal, equivalence, addition of fractions", 1,
                L("What is a fraction?", LessonType.Note,
                    "A fraction represents equal parts of a whole. The bottom number (denominator) tells how many equal parts the whole is divided into, and the top number (numerator) tells how many parts we take. Two fractions are equivalent when they name the same amount, for example 1/2 = 2/4 = 3/6. To add fractions we first rewrite them with a common denominator, then add the numerators.", 15, 1),
                L("Converting between fractions and decimals", LessonType.Note,
                    "Every fraction can be written as a decimal by dividing the numerator by the denominator. Terminating decimals such as 0.25 come from denominators built from 2s and 5s; repeating decimals such as 0.333... come from denominators containing 3, 7 or other primes. Knowing the equivalence lets you choose whichever form makes a problem easier.", 20, 2)),
            T("Ratios & Proportion", "ratio, proportion, unit rate, scaling", 2,
                L("Understanding ratios", LessonType.Note,
                    "A ratio compares two quantities by division. A ratio of 3:2 in a fruit punch means 3 cups of juice for every 2 cups of water, and the order matters. Ratios can be scaled up or down by multiplying both sides by the same number, which is the heart of every proportion problem.", 15, 1),
                L("Unit rates in daily life", LessonType.Note,
                    "A unit rate compares a quantity to one unit of another quantity, like price per kilogram or kilometres per litre. To find it, divide the first quantity by the second. Unit rates let you compare deals fairly — 300 birr for 2 kg and 440 birr for 3 kg are not equally cheap.", 20, 2)));
        M(math, "Algebra Basics", 2,
            T("Linear Equations", "linear equation, variable, balance method, inverse operations", 1,
                L("One-step equations", LessonType.Note,
                    "An equation is a balance scale: whatever you do to one side you must do to the other. To solve x + 5 = 12 subtract 5 from both sides; to solve 3x = 15 divide both sides by 3. The goal is always to isolate the variable.", 15, 1),
                L("Two-step equations", LessonType.Note,
                    "For 2x + 3 = 11 we undo operations in reverse order: first subtract 3 from both sides, then divide by 2, giving x = 4. Always check by substituting back into the original equation — both sides must match.", 20, 2)),
            T("Inequalities", "inequality, number line, reversing the sign", 3,
                L("Solving and graphing inequalities", LessonType.Note,
                    "Inequalities such as x - 2 > 5 are solved almost like equations: x > 7. We draw the solution on a number line with an open circle for < or > and a closed circle for ≤ or ≥. The one big difference: multiplying or dividing by a negative number reverses the inequality sign.", 15, 1)));

        var physics = C("Physics Foundations", "Physics", "Grade 10", 500m, 0m, daniel, 200,
            "Motion, forces and energy — build the physical intuition and problem-solving habits for Grade 10 physics.");
        M(physics, "Mechanics", 1,
            T("Motion & Velocity", "displacement, velocity, acceleration, graphs of motion", 1,
                L("Describing motion", LessonType.Note,
                    "Displacement is the change of position in a straight line from start to end, while distance is the total path length. Velocity is displacement per unit time and carries direction; acceleration is the rate of change of velocity. Distance-time graphs rising steeply mean fast motion; velocity-time graphs with a constant slope mean uniform acceleration.", 20, 1)),
            T("Newton's Laws", "inertia, net force, action-reaction pairs", 2,
                L("The three laws", LessonType.Note,
                    "First law: an object keeps its state of rest or uniform motion unless a net force acts — that tendency is inertia. Second law: net force equals mass times acceleration (F = ma). Third law: forces always come in equal and opposite action-reaction pairs acting on different bodies.", 20, 1),
                L("Free-body diagrams", LessonType.Note,
                    "A free-body diagram draws every force acting on one chosen object as an arrow from its centre. Tension, normal force, friction and weight each get their own arrow. Adding them as vectors gives the net force you insert into F = ma.", 15, 2)));
        M(physics, "Energy", 2,
            T("Work & Energy", "work, kinetic energy, potential energy, conservation", 1,
                L("Work, energy and power", LessonType.Note,
                    "Work is force times displacement in the direction of the force. Kinetic energy is ½mv², gravitational potential energy is mgh, and in a closed system the total mechanical energy stays constant — energy only changes form. Power is the rate of doing work, measured in watts.", 25, 1)));

        var biology = C("Biology Essentials", "Biology", "Grade 9", 400m, 10m, hanna, 140,
            "From cells to body systems — a clear, illustrated path through the Grade 9 biology syllabus.");
        M(biology, "Cells", 1,
            T("Cell Structure", "cell membrane, nucleus, mitochondria, organelles", 1,
                L("Inside a cell", LessonType.Note,
                    "Every living cell is enclosed by a cell membrane that controls what enters and leaves. The nucleus stores DNA and directs activities; mitochondria release energy from food; chloroplasts in plant cells capture light for photosynthesis. Organelles divide the labour of life, just like organs in a body.", 20, 1)),
            T("Cell Transport", "diffusion, osmosis, active transport", 2,
                L("Moving substances in and out", LessonType.Note,
                    "Diffusion moves particles from high to low concentration without energy; osmosis is the diffusion of water across a selectively permeable membrane. Active transport moves substances against the gradient and spends ATP — the cell paying energy to concentrate what it needs.", 20, 1)));
        M(biology, "Human Body", 2,
            T("Circulatory System", "heart, blood vessels, double circulation", 1,
                L("The heart as a double pump", LessonType.Note,
                    "The heart has four chambers and acts as a double pump: the right side pushes blood to the lungs to collect oxygen, the left side pushes oxygen-rich blood to the whole body. Valves keep blood flowing in one direction; arteries carry blood away from the heart and veins return it.", 20, 1)));

        var python = C("Introduction to Python Programming", "Computer Science", "College Year 1", 1200m, 15m, daniel, 30,
            "Your first semester of coding: variables, control flow, functions and data structures, with weekly practice labs.");
        M(python, "Python Basics", 1,
            T("Variables & Types", "variable, integer, string, float, type conversion", 1,
                L("Storing values", LessonType.Note,
                    "A variable is a named box that stores a value. Python infers the type automatically: x = 7 makes an int, name = \"Abi\" makes a str, price = 19.99 makes a float. You can convert between types with int(), float() and str(). Clear variable names make programs readable.", 20, 1),
                L("Control flow", LessonType.Note,
                    "Programs make decisions with if / elif / else and repeat work with for and while loops. Indentation defines the block structure in Python — four spaces per level. The range() function generates number sequences used to drive for loops.", 25, 2)),
            T("Functions", "function, parameter, return value, scope", 2,
                L("Defining and calling functions", LessonType.Note,
                    "A function packages reusable steps under one name: def greet(name): return f\"Hello {name}\". Parameters are the inputs listed in the definition; arguments are the actual values passed when calling. A return value hands a result back to the caller; variables defined inside a function are local to it.", 25, 1)));
        M(python, "Data Structures", 2,
            T("Lists & Dictionaries", "list, dictionary, index, key-value pair", 1,
                L("Working with collections", LessonType.Note,
                    "Lists keep items in order and are indexed from zero: scores[0] is the first element. Dictionaries store key-value pairs for direct lookup: ages[\"Abi\"] = 15. Use lists when order matters, dictionaries when you want to find things by a meaningful key instead of a position.", 25, 1)));

        var calculus = C("Algebra & Calculus I", "Mathematics", "University Year 1", 1500m, 0m, sara, 275,
            "University-level foundations: functions, limits, derivatives and an introduction to integrals.");
        M(calculus, "Limits & Derivatives", 1,
            T("Limits", "limit, continuity, one-sided limit", 1,
                L("The idea of a limit", LessonType.Note,
                    "A limit describes the value a function approaches as the input approaches a point — even if the function never reaches it there. Limits formalise continuity: a function is continuous at a point when the limit equals the actual value. One-sided limits guard against jumps.", 25, 1)),
            T("Derivatives", "derivative, rate of change, differentiation rules", 2,
                L("Slope as a derivative", LessonType.Note,
                    "The derivative f'(x) measures instantaneous rate of change — the slope of the tangent line. Power rule: d/dx xⁿ = n·xⁿ⁻¹. Product, quotient and chain rules let us differentiate combinations. Derivatives answer physics questions like velocity from position.", 30, 1),
                L("Watch: The essence of calculus", LessonType.Video, "", 15, 2, "WUvTyaaNkzM")));
        M(calculus, "Integrals", 2,
            T("Antiderivatives", "antiderivative, indefinite integral, area under curve", 1,
                L("Reversing the derivative", LessonType.Note,
                    "An antiderivative reverses differentiation: if F'(x) = f(x) then F is an antiderivative of f. The definite integral sums infinitely many thin rectangles and equals the signed area under a curve — the Fundamental Theorem links integration and differentiation into one story.", 30, 1)));

        var chemistry = C("Chemistry Basics", "Chemistry", "Grade 10", 480m, 0m, hanna, 90,
            "Atoms, bonding and reactions — the Grade 10 chemistry core with lab-style demonstrations.");
        M(chemistry, "Atoms & Bonding", 1,
            T("Atomic Structure", "proton, neutron, electron, periodic table", 1,
                L("Inside the atom", LessonType.Note,
                    "Atoms contain a nucleus of protons and neutrons surrounded by electrons in shells. The atomic number equals the proton count and fixes the element's place in the periodic table; electron shells explain why elements in the same column behave alike.", 20, 1)));

        chemistry.Status = CourseStatus.Draft; // demo: visible only in the admin back office

        db.Courses.AddRange(math, physics, biology, python, calculus, chemistry);

        // ── Historical paid orders (revenue ledger demo) ─────────────────────────────
        const decimal rate = 22m; // matches the seeded PlatformSetting
        db.Orders.AddRange(
            O(abi, math, 4, rate, "telebirr_mock"),
            O(abi, python, 3, rate, "chapa_card_mock"),
            O(selam, physics, 2, rate, "cbebirr_mock"),
            O(selam, math, 2, rate, "telebirr_mock"),
            O(abi, calculus, 1, rate, "mpesa_mock"));

        db.Enrollments.AddRange(
            new Enrollment { Student = abi, Course = math },
            new Enrollment { Student = abi, Course = python },
            new Enrollment { Student = abi, Course = calculus },
            new Enrollment { Student = selam, Course = physics },
            new Enrollment { Student = selam, Course = math });

        db.Users.AddRange(admin, sara, daniel, hanna, abi, selam);
        await db.SaveChangesAsync();
    }

    private static Course C(string title, string subject, string level, decimal price, decimal discount, User teacher, int hue, string summary) =>
        new()
        {
            Title = title,
            Slug = Slug(title),
            Subject = subject,
            Level = level,
            Price = price,
            DiscountPercent = discount,
            CoverHue = hue,
            Teacher = teacher,
            Summary = summary,
            Status = CourseStatus.Published
        };

    private static Module M(Course course, string title, int index, params Topic[] topics)
    {
        var module = new Module { Course = course, Title = title, OrderIndex = index };
        foreach (var topic in topics) module.Topics.Add(topic);
        course.Modules.Add(module);
        return module;
    }

    private static Topic T(string title, string concepts, int index, params Lesson[] lessons)
    {
        var topic = new Topic { Title = title, Concepts = concepts, OrderIndex = index };
        foreach (var lesson in lessons) topic.Lessons.Add(lesson);
        return topic;
    }

    private static Lesson L(string title, LessonType type, string content, int minutes, int index, string? video = null) =>
        new() { Title = title, Type = type, Content = content, DurationMinutes = minutes, OrderIndex = index, VideoYoutubeId = video };

    private static Order O(User student, Course course, int monthsAgo, decimal rate, string provider)
    {
        var paidAt = DateTime.UtcNow.AddMonths(-monthsAgo);
        var final = course.FinalPrice();
        var commission = Math.Round(final * rate / 100m, 2, MidpointRounding.AwayFromZero);
        return new Order
        {
            Student = student,
            Course = course,
            CourseTitle = course.Title,
            ProviderCode = provider,
            ProviderRef = $"SEED-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}",
            GrossAmount = course.Price,
            DiscountAmount = course.Price - final,
            FinalAmount = final,
            CommissionRatePercent = rate,
            CommissionAmount = commission,
            NetAmount = final - commission,
            Status = OrderStatus.Paid,
            CreatedAt = paidAt,
            PaidAt = paidAt
        };
    }

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex SlugChars();

    private static string Slug(string title) =>
        SlugChars().Replace(title.ToLowerInvariant(), "-").Trim('-');
}
