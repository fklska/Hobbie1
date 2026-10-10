using System.Collections.Generic;
using System.Linq;

public static class HeroClasses
{
	public const string Warrior = "warrior";
	public const string Mage = "mage";
	public const string Druid = "druid";
	public const string SlotAttack = "attack";
	public static readonly string[] SlotActions = { "skill_1", "skill_2", "skill_3" };
	public static readonly string[] SlotKeys = { "Q", "R", "C" };

	public class Skill
	{
		public string Id;
		public string Title;
		public int Branch;
		public int Tier;
		public int[] Costs;
		public string Requires = "";
		public int Slot = -1;
		public float Cooldown;
		public string Description;
		public string[] Ranks;
		public int MaxRank => Costs.Length;
		public bool Active => Slot >= 0;
	}

	public class ClassInfo
	{
		public string Id;
		public string Title;
		public string Description;
		public string Frames;
		public int BonusHealth;
		public float AttackScale;
		public string[] Branches;
		public Skill[] Skills;
		public string Innate;
	}

	public static readonly Dictionary<string, ClassInfo> Classes = new()
	{
		[Warrior] = new ClassInfo
		{
			Id = Warrior,
			Title = "Воин",
			Description = "Ближний бой и много здоровья. Быстрая серия слабых ударов или медленные, но сокрушительные удары.",
			Frames = "res://Art/hero/hero_frames.tres",
			BonusHealth = 30,
			AttackScale = 1f,
			Branches = new[] { "Быстрые удары", "Тяжёлые удары" },
			Innate = "combo",
			Skills = new[]
			{
				new Skill
				{
					Id = "combo", Title = "Серия ударов", Branch = 0, Tier = 1, Costs = new[] { 0, 1, 2 },
					Description = "Обычная атака (F, ПКМ) бьёт часто и слабо, каждый третий удар двойной и шире. Атаку можно держать.",
					Ranks = new[] { "45% урона за удар", "55% урона за удар, чаще", "65% урона за удар, ещё чаще" },
				},
				new Skill
				{
					Id = "whirl", Title = "Вихрь", Branch = 0, Tier = 2, Costs = new[] { 2, 2 }, Requires = "combo", Slot = 1, Cooldown = 9f,
					Description = "Герой кружится с оружием и бьёт всех врагов вокруг. Двигаться можно, но медленнее.",
					Ranks = new[] { "4 удара по 60% урона", "6 ударов по 70% урона" },
				},
				new Skill
				{
					Id = "heavy", Title = "Тяжёлый удар", Branch = 1, Tier = 1, Costs = new[] { 1, 1, 2 }, Slot = 0, Cooldown = 4f,
					Description = "Замах почти секунду, герой стоит на месте. Потом мощный удар полукругом отбрасывает врагов.",
					Ranks = new[] { "250% урона", "300% урона", "350% урона" },
				},
				new Skill
				{
					Id = "quake", Title = "Сотрясение", Branch = 1, Tier = 2, Costs = new[] { 3, 3 }, Requires = "heavy", Slot = 2, Cooldown = 14f,
					Description = "Удар оружием о землю: волна по кругу ранит, отбрасывает и оглушает всех врагов рядом.",
					Ranks = new[] { "200% урона, оглушение 1.5 с", "260% урона, оглушение 2.2 с" },
				},
			},
		},
		[Mage] = new ClassInfo
		{
			Id = Mage,
			Title = "Маг",
			Description = "Заклинания стихий бьют по площади издалека. Пока открыта стихия огня: поджог и падение метеорита.",
			Frames = "res://Art/hero/mage_frames.tres",
			BonusHealth = 0,
			AttackScale = 0.6f,
			Branches = new[] { "Огонь", "Мастерство" },
			Innate = "ignite",
			Skills = new[]
			{
				new Skill
				{
					Id = "ignite", Title = "Поджог", Branch = 0, Tier = 1, Costs = new[] { 0, 1, 2 }, Slot = 0, Cooldown = 3f,
					Description = "Огненный шар летит в точку под курсором, взрывается и поджигает врагов вокруг.",
					Ranks = new[] { "горение 4 с", "горение 5 с", "горение 6 с" },
				},
				new Skill
				{
					Id = "pyre", Title = "Пламя пожирает", Branch = 0, Tier = 2, Costs = new[] { 2, 2 }, Requires = "ignite",
					Description = "Горящий враг, погибая, поджигает всех врагов рядом.",
					Ranks = new[] { "радиус 110", "радиус 170" },
				},
				new Skill
				{
					Id = "meteor", Title = "Падение метеорита", Branch = 0, Tier = 3, Costs = new[] { 3, 3 }, Requires = "pyre", Slot = 1, Cooldown = 25f,
					Description = "На земле растёт тень, затем с неба падает раскалённый метеорит. Он сжигает врагов и уничтожает деревья и камни под собой. Свою деревню не задевает.",
					Ranks = new[] { "300% урона + 60", "400% урона + 90, перезарядка 20 с" },
				},
				new Skill
				{
					Id = "heat", Title = "Жар", Branch = 1, Tier = 1, Costs = new[] { 1, 1, 2 },
					Description = "Огненные заклинания и горение наносят больше урона.",
					Ranks = new[] { "+15% урона огнём", "+30% урона огнём", "+45% урона огнём" },
				},
				new Skill
				{
					Id = "focus", Title = "Сосредоточенность", Branch = 1, Tier = 2, Costs = new[] { 2, 2 }, Requires = "heat",
					Description = "Заклинания перезаряжаются быстрее.",
					Ranks = new[] { "−10% перезарядки", "−20% перезарядки" },
				},
			},
		},
		[Druid] = new ClassInfo
		{
			Id = Druid,
			Title = "Друид",
			Description = "Призывает зверей, которые сражаются за него: сначала ворона, потом волка, потом медведя.",
			Frames = "res://Art/hero/druid_frames.tres",
			BonusHealth = 10,
			AttackScale = 0.7f,
			Branches = new[] { "Звери", "Дикая сила" },
			Innate = "crow",
			Skills = new[]
			{
				new Skill
				{
					Id = "crow", Title = "Ворон", Branch = 0, Tier = 1, Costs = new[] { 0, 1, 2 }, Slot = 0, Cooldown = 15f,
					Description = "Вороны клюют врагов рядом с героем. Враги не могут их атаковать. Живут 30 с.",
					Ranks = new[] { "1 ворон", "2 ворона", "3 ворона" },
				},
				new Skill
				{
					Id = "wolf", Title = "Волк", Branch = 0, Tier = 2, Costs = new[] { 2, 2 }, Requires = "crow", Slot = 1, Cooldown = 30f,
					Description = "Волк быстро бегает и отвлекает врагов на себя. Живёт 45 с.",
					Ranks = new[] { "1 волк", "2 волка" },
				},
				new Skill
				{
					Id = "bear", Title = "Медведь", Branch = 0, Tier = 3, Costs = new[] { 3, 3 }, Requires = "wolf", Slot = 2, Cooldown = 60f,
					Description = "Самый сильный зверь: много здоровья, бьёт лапой всех врагов перед собой. Живёт 45 с.",
					Ranks = new[] { "260 здоровья", "360 здоровья, живёт 60 с" },
				},
				new Skill
				{
					Id = "wild", Title = "Дикая сила", Branch = 1, Tier = 1, Costs = new[] { 1, 1, 2 },
					Description = "Звери сильнее и крепче.",
					Ranks = new[] { "+20% урона и здоровья", "+40% урона и здоровья", "+60% урона и здоровья" },
				},
				new Skill
				{
					Id = "bond", Title = "Единение", Branch = 1, Tier = 2, Costs = new[] { 2, 2 }, Requires = "wild",
					Description = "Звери живут дольше, а герой восстанавливает здоровье, пока рядом есть звери.",
					Ranks = new[] { "+30% времени, 2 здоровья в секунду", "+60% времени, 4 здоровья в секунду" },
				},
			},
		},
	};

	public static ClassInfo Get(string id) => id != null && Classes.TryGetValue(id, out ClassInfo info) ? info : null;

	public static Skill FindSkill(string classId, string skillId) => Get(classId)?.Skills.FirstOrDefault(s => s.Id == skillId);
}
