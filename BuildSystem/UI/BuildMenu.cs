using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class BuildMenu : TabContainer
{
	private static readonly Color LockColor = new(1f, 0.55f, 0.45f);
	private static readonly Dictionary<(string, int), Texture2D> iconCache = new();

	private readonly List<Action> refreshers = new();
	private GameManager game;

	public override void _Ready()
	{
		game = GameManager.Instance;
		AddToGroup("closable_ui");
		BuildBuildingsTab(AddTab("Постройки"));
		BuildVillageTab(AddTab("Деревня"));
		BuildArsenalTab(AddTab("Арсенал"));
		BuildMarketTab(AddTab("Рынок"));
		CurrentTab = 0;

		game.ProgressChanged += Refresh;
		game.StockChanged += Refresh;
		VisibilityChanged += Refresh;
		Refresh();
	}

	public override void _ExitTree()
	{
		game.ProgressChanged -= Refresh;
		game.StockChanged -= Refresh;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("menu")) return;
		Visible = !Visible;
		GetViewport().SetInputAsHandled();
	}

	public void Refresh()
	{
		if (!Visible) return;
		foreach (Action refresh in refreshers) refresh();
	}

	public void OpenMenu() => Visible = true;

	public void CloseMenu() => Visible = false;

	private VBoxContainer AddTab(string title)
	{
		ScrollContainer scroll = new() { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		VBoxContainer list = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		list.AddThemeConstantOverride("separation", 6);
		scroll.AddChild(list);
		AddChild(scroll);
		SetTabTitle(GetTabCount() - 1, title);
		return list;
	}

	private record Card(Label Title, Label Info, Label Lock, HBoxContainer Buttons, TextureRect Icon);

	private static Card AddCard(VBoxContainer list, Texture2D icon)
	{
		PanelContainer panel = new() { ThemeTypeVariation = "InsetPanel" };
		HBoxContainer row = new();
		row.AddThemeConstantOverride("separation", 10);
		TextureRect iconRect = new()
		{
			Texture = icon,
			CustomMinimumSize = new Vector2(56, 56),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			Visible = icon != null,
		};
		VBoxContainer text = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		Label title = new() { ThemeTypeVariation = "SubheaderLabel" };
		Label info = new() { ThemeTypeVariation = "SubtleLabel", AutowrapMode = TextServer.AutowrapMode.WordSmart };
		Label lockLabel = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart };
		lockLabel.AddThemeColorOverride("font_color", LockColor);
		HBoxContainer buttons = new() { Alignment = BoxContainer.AlignmentMode.End };
		text.AddChild(title);
		text.AddChild(info);
		text.AddChild(lockLabel);
		text.AddChild(buttons);
		row.AddChild(iconRect);
		row.AddChild(text);
		panel.AddChild(row);
		list.AddChild(panel);
		return new Card(title, info, lockLabel, buttons, iconRect);
	}

	private static Button AddButton(Container parent, Action pressed)
	{
		Button button = new() { FocusMode = FocusModeEnum.None };
		button.Pressed += pressed;
		parent.AddChild(button);
		return button;
	}

	private static Label AddHeader(VBoxContainer list, string text)
	{
		Label label = new() { Text = text, ThemeTypeVariation = "HeaderLabel", HorizontalAlignment = HorizontalAlignment.Center };
		list.AddChild(label);
		return label;
	}

	private static void SetLock(Card card, string reason)
	{
		card.Lock.Text = reason;
		card.Lock.Visible = reason != "";
	}

	private static Texture2D BuildingIcon(string id, int level)
	{
		if (iconCache.TryGetValue((id, level), out Texture2D cached)) return cached;
		Node building = GD.Load<PackedScene>(Economy.Buildings[id].ScenePath).Instantiate();
		Godot.Collections.Dictionary preview = building.Call("preview", level).AsGodotDictionary();
		building.Free();
		Texture2D icon = preview.Count > 0 ? preview["texture"].As<Texture2D>() : null;
		iconCache[(id, level)] = icon;
		return icon;
	}

	private void BuildBuildingsTab(VBoxContainer list)
	{
		AddHeader(list, "Центр поселения");
		Card core = AddCard(list, null);
		Button upgradeCore = AddButton(core.Buttons, () => game.UpgradeBuilding(Economy.Core));
		refreshers.Add(() =>
		{
			Economy.Level level = game.LevelInfo(Economy.Core);
			Economy.Level next = game.NextLevel(Economy.Core);
			core.Icon.Texture = BuildingIcon(Economy.Core, game.CoreLevel);
			core.Icon.Visible = core.Icon.Texture != null;
			core.Title.Text = $"{level.Title} (ур. {game.CoreLevel}/{Economy.Buildings[Economy.Core].Levels.Length})";
			core.Info.Text = $"{Economy.Buildings[Economy.Core].Description}. Жители {game.Workers.Count}/{game.PopulationCap}, склад {game.Capacity("wood")}, ополчение {game.MilitiaCap}";
			UpdateUpgrade(core, upgradeCore, Economy.Core, next);
		});

		AddHeader(list, "Постройки");
		foreach (string id in Economy.BuildOrder)
		{
			Card card = AddCard(list, null);
			Button build = AddButton(card.Buttons, () => StartPlacement(id));
			build.ThemeTypeVariation = "PrimaryButton";
			Button upgrade = AddButton(card.Buttons, () => game.UpgradeBuilding(id));
			refreshers.Add(() =>
			{
				Economy.Level level = game.LevelInfo(id);
				card.Icon.Texture = BuildingIcon(id, game.GetLevel(id));
				card.Icon.Visible = card.Icon.Texture != null;
				card.Title.Text = $"{level.Title} (ур. {game.GetLevel(id)}) — {game.CountOf(id)}/{game.MaxCount(id)}";
				card.Info.Text = $"{Economy.Buildings[id].Description}. {Capitalize(Economy.EffectText(level))}";
				string lockReason = game.BuildLock(id);
				build.Text = $"Построить ({Economy.CostText(level.Cost)})";
				build.Disabled = lockReason != "" || !game.CanAfford(level.Cost);
				UpdateUpgrade(card, upgrade, id, game.NextLevel(id), lockReason);
			});
		}
	}

	private void UpdateUpgrade(Card card, Button button, string id, Economy.Level next, string buildLock = "")
	{
		string upgradeLock = game.UpgradeLock(id);
		button.Visible = next != null;
		if (next != null)
		{
			int times = game.UpgradeTimes(id);
			button.Text = $"Улучшить до «{next.Title}» ({Economy.CostText(next.Cost, times)})";
			button.Disabled = upgradeLock != "" || !game.CanAfford(next.Cost, times);
			button.TooltipText = Capitalize(Economy.EffectText(next));
		}
		string reason = buildLock != "" && buildLock != "Уже построено" && !buildLock.StartsWith("Предел") ? buildLock
			: next != null && game.HasBuilding(id) && upgradeLock != "" ? $"Для улучшения: {char.ToLower(upgradeLock[0])}{upgradeLock[1..]}" : "";
		SetLock(card, reason);
	}

	private void StartPlacement(string id)
	{
		CloseMenu();
		Grid.StartPlacement(id);
	}

	private void BuildVillageTab(VBoxContainer list)
	{
		AddHeader(list, "Жители");
		Card people = AddCard(list, null);
		Button hire = AddButton(people.Buttons, game.HireWorker);
		hire.Text = $"Нанять жителя ({Economy.CostText(Economy.HireCost)})";

		HBoxContainer job = new();
		job.AddChild(new Label { Text = "Жители добывают:" });
		OptionButton jobOption = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill, FocusMode = FocusModeEnum.None };
		foreach (string kind in Economy.Gatherable) jobOption.AddItem(Economy.ResourceTitles[kind]);
		jobOption.ItemSelected += index => game.SetWorkerJob(Economy.Gatherable[index]);
		job.AddChild(jobOption);
		list.AddChild(job);

		refreshers.Add(() =>
		{
			people.Title.Text = $"Жители: {game.Workers.Count}/{game.PopulationCap}";
			people.Info.Text = $"Еда: {game.GetStock("food")}, урожай +{game.FoodPerDay}, съедают {game.FoodUpkeep} каждое утро. " +
				$"Налоги +{game.TaxPerDay} монет в день. Скорость работы ×{game.WorkSpeed:0.#}";
			SetLock(people, game.Hungry ? "Жители голодают и работают вдвое медленнее" : game.Workers.Count >= game.PopulationCap ? "Нет свободного жилья" : "");
			hire.Disabled = game.Workers.Count >= game.PopulationCap || !game.CanAfford(Economy.HireCost);
			jobOption.Selected = Array.IndexOf(Economy.Gatherable, game.WorkerJob);
		});

		AddHeader(list, "Ополчение");
		Card militia = AddCard(list, null);
		Button disarm = AddButton(militia.Buttons, game.DisarmWorkers);
		disarm.Text = "Снять оружие со всех";
		GridContainer armButtons = new() { Columns = 2 };
		list.AddChild(armButtons);
		Dictionary<string, Button> arm = new();
		foreach (string id in Economy.MeleeWeapons.Concat(Economy.RangedWeapons))
			arm[id] = AddButton(armButtons, () => game.ArmNextWorker(id));

		refreshers.Add(() =>
		{
			militia.Title.Text = $"Вооружено: {game.ArmedWorkers}/{game.MilitiaCap}";
			militia.Info.Text = "Жители берут оружие из арсенала и защищают деревню. Предел растёт с уровнем центра и казармой";
			SetLock(militia, game.Armory.Values.Sum() == 0 ? "Выкуйте оружие во вкладке «Арсенал»" : "");
			disarm.Disabled = game.ArmedWorkers == 0;
			foreach (var (id, button) in arm)
			{
				button.Visible = game.ArmoryCount(id) > 0;
				button.Text = $"Вооружить: {Economy.Weapons[id].Title.ToLower()} (×{game.ArmoryCount(id)})";
				button.Disabled = game.ArmedWorkers >= game.MilitiaCap || game.ArmedWorkers >= game.Workers.Count;
			}
		});
	}

	private void BuildArsenalTab(VBoxContainer list)
	{
		AddHeader(list, "Снаряжение");
		AddTierCard(list, "Инструменты", Economy.Tools, () => game.ToolTier, game.ToolLock, game.UpgradeTools, t => $"скорость добычи ×{t.Value:0.#}");
		AddTierCard(list, "Доспех героя", Economy.Armor, () => game.ArmorTier, game.ArmorLock, game.UpgradeArmor, t => $"здоровье героя {t.Value}");

		Card hero = AddCard(list, null);
		refreshers.Add(() =>
		{
			Economy.WeaponInfo weapon = Economy.Weapons[game.HeroWeapon];
			hero.Icon.Texture = GD.Load<Texture2D>(weapon.Icon);
			hero.Icon.Visible = true;
			hero.Title.Text = $"Оружие героя: {weapon.Title}";
			hero.Info.Text = $"Урон {game.HeroDamage}. Выкуйте оружие ближнего боя ниже и нажмите «Герою»";
			SetLock(hero, "");
		});

		AddHeader(list, "Ближний бой (кузница)");
		foreach (string id in Economy.MeleeWeapons) AddWeaponCard(list, id);
		AddHeader(list, "Дальний бой (мастерская)");
		foreach (string id in Economy.RangedWeapons) AddWeaponCard(list, id);
	}

	private void AddTierCard(VBoxContainer list, string name, Economy.Tier[] tiers, Func<int> current, Func<string> lockReason, Action upgrade, Func<Economy.Tier, string> effect)
	{
		Card card = AddCard(list, null);
		Button button = AddButton(card.Buttons, upgrade);
		refreshers.Add(() =>
		{
			int tier = current();
			card.Title.Text = $"{name}: {tiers[tier].Title}";
			card.Info.Text = Capitalize(effect(tiers[tier]));
			bool last = tier + 1 >= tiers.Length;
			button.Visible = !last;
			string reason = lockReason();
			SetLock(card, last ? "" : reason);
			if (last) return;
			Economy.Tier next = tiers[tier + 1];
			button.Text = $"{next.Title} ({Economy.CostText(next.Cost)})";
			button.TooltipText = Capitalize(effect(next));
			button.Disabled = reason != "" || !game.CanAfford(next.Cost);
		});
	}

	private void AddWeaponCard(VBoxContainer list, string id)
	{
		Economy.WeaponInfo weapon = Economy.Weapons[id];
		Card card = AddCard(list, GD.Load<Texture2D>(weapon.Icon));
		Button forge = AddButton(card.Buttons, () => game.ForgeWeapon(id));
		Button forgeFive = AddButton(card.Buttons, () => game.ForgeWeapon(id, 5));
		forgeFive.Text = "×5";
		Button equip = weapon.Kind == WeaponKind.Melee ? AddButton(card.Buttons, () => game.EquipHero(id)) : null;
		if (equip != null) equip.Text = "Герою";
		refreshers.Add(() =>
		{
			card.Title.Text = $"{weapon.Title} — в арсенале {game.ArmoryCount(id)}";
			card.Info.Text = Capitalize(Economy.WeaponText(weapon));
			string reason = game.WeaponLock(id);
			SetLock(card, reason);
			forge.Text = $"Выковать ({Economy.CostText(weapon.Cost)})";
			forge.Disabled = reason != "" || !game.CanAfford(weapon.Cost);
			forgeFive.Disabled = reason != "" || !game.CanAfford(weapon.Cost, 5);
			if (equip != null) equip.Disabled = game.HeroWeapon == id || game.ArmoryCount(id) == 0;
		});
	}

	private void BuildMarketTab(VBoxContainer list)
	{
		AddHeader(list, "Торговля");
		Card market = AddCard(list, null);
		refreshers.Add(() =>
		{
			bool open = game.HasBuilding("Market");
			market.Title.Text = open ? game.LevelInfo("Market").Title : "Рынка нет";
			market.Info.Text = $"Монеты: {game.GetStock(Economy.Coins)}. Налоги +{game.TaxPerDay} в день. Торговля партиями по {Economy.TradeLot}";
			SetLock(market, open ? "" : "Постройте меновой двор, чтобы торговать и собирать налоги");
		});

		foreach (string kind in Economy.Tradable)
		{
			Card card = AddCard(list, null);
			Button sell = AddButton(card.Buttons, () => game.Sell(kind));
			Button buy = AddButton(card.Buttons, () => game.Buy(kind));
			refreshers.Add(() =>
			{
				bool open = game.HasBuilding("Market");
				card.Title.Text = $"{Economy.ResourceTitles[kind]}: {game.GetStock(kind)}/{game.Capacity(kind)}";
				card.Info.Visible = false;
				SetLock(card, "");
				sell.Text = $"Продать {Economy.TradeLot} (+{game.SellPrice(kind)})";
				buy.Text = $"Купить {Economy.TradeLot} (−{game.BuyPrice(kind)})";
				sell.Disabled = !open || game.GetStock(kind) < Economy.TradeLot;
				buy.Disabled = !open || game.GetStock(Economy.Coins) < game.BuyPrice(kind) || game.GetStock(kind) + Economy.TradeLot > game.Capacity(kind);
			});
		}
	}

	private static string Capitalize(string text) => text.Length == 0 ? text : char.ToUpper(text[0]) + text[1..];
}
