using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Engine;

namespace Game;

public static class ExternalContentManager
{
	public static List<IExternalContentProvider> m_providers;

	public static string fName = "ExternalContentManager";

	public static IExternalContentProvider DefaultProvider
	{
		get
		{
			if (Providers.Count <= 0)
			{
				return null;
			}
			return Providers[0];
		}
	}

	public static ReadOnlyList<IExternalContentProvider> Providers => new ReadOnlyList<IExternalContentProvider>(m_providers);

	public static void Initialize()
	{
		m_providers = new List<IExternalContentProvider>
		{
			new SPMBoxExternalContentProvider(),
			new DiskExternalContentProvider(),
			new DropboxExternalContentProvider(),
			new TransferShExternalContentProvider()
		};
	}

	public static ExternalContentType ExtensionToType(string extension)
	{
		extension = extension.ToLower();
		foreach (ExternalContentType value in Enum.GetValues(typeof(ExternalContentType)))
		{
			if (GetEntryTypeExtensions(value).FirstOrDefault((string e) => e == extension) != null)
			{
				return value;
			}
		}
		return ExternalContentType.Unknown;
	}

	public static IEnumerable<string> GetEntryTypeExtensions(ExternalContentType type)
	{
		switch (type)
		{
		case ExternalContentType.World:
			yield return ".scworld";
			break;
		case ExternalContentType.BlocksTexture:
			yield return ".scbtex";
			yield return ".png";
			break;
		case ExternalContentType.CharacterSkin:
			yield return ".scskin";
			break;
		case ExternalContentType.FurniturePack:
			yield return ".scfpack";
			break;
		}
	}

	public static Subtexture GetEntryTypeIcon(ExternalContentType type)
	{
		return type switch
		{
			ExternalContentType.Directory => ContentManager.Get<Subtexture>("Textures/Atlas/FolderIcon"), 
			ExternalContentType.World => ContentManager.Get<Subtexture>("Textures/Atlas/WorldIcon"), 
			ExternalContentType.BlocksTexture => ContentManager.Get<Subtexture>("Textures/Atlas/TexturePackIcon"), 
			ExternalContentType.CharacterSkin => ContentManager.Get<Subtexture>("Textures/Atlas/CharacterSkinIcon"), 
			ExternalContentType.FurniturePack => ContentManager.Get<Subtexture>("Textures/Atlas/FurnitureIcon"), 
			_ => ContentManager.Get<Subtexture>("Textures/Atlas/QuestionMarkIcon"), 
		};
	}

	public static string GetEntryTypeDescription(ExternalContentType type)
	{
		return type switch
		{
			ExternalContentType.Directory => LanguageControl.Get(fName, "Directory"), 
			ExternalContentType.World => LanguageControl.Get(fName, "World"), 
			ExternalContentType.BlocksTexture => LanguageControl.Get(fName, "Blocks Texture"), 
			ExternalContentType.CharacterSkin => LanguageControl.Get(fName, "Character Skin"), 
			ExternalContentType.FurniturePack => LanguageControl.Get(fName, "Furniture Pack"), 
			_ => string.Empty, 
		};
	}

	public static bool IsEntryTypeDownloadSupported(ExternalContentType type)
	{
		return type switch
		{
			ExternalContentType.World => true, 
			ExternalContentType.BlocksTexture => true, 
			ExternalContentType.CharacterSkin => true, 
			ExternalContentType.FurniturePack => true, 
			_ => false, 
		};
	}

	public static bool DoesEntryTypeRequireName(ExternalContentType type)
	{
		return type switch
		{
			ExternalContentType.BlocksTexture => true, 
			ExternalContentType.CharacterSkin => true, 
			ExternalContentType.FurniturePack => true, 
			_ => false, 
		};
	}

	public static Exception VerifyExternalContentName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return new InvalidOperationException(LanguageControl.Get(fName, 1));
		}
		if (name.Length > 50)
		{
			return new InvalidOperationException(LanguageControl.Get(fName, 2));
		}
		if (name[0] == ' ' || name[name.Length - 1] == ' ')
		{
			return new InvalidOperationException(LanguageControl.Get(fName, 3));
		}
		return null;
	}

	public static void DeleteExternalContent(ExternalContentType type, string name)
	{
		switch (type)
		{
		case ExternalContentType.World:
			WorldsManager.DeleteWorld(name);
			break;
		case ExternalContentType.BlocksTexture:
			BlocksTexturesManager.DeleteBlocksTexture(name);
			break;
		case ExternalContentType.CharacterSkin:
			CharacterSkinsManager.DeleteCharacterSkin(name);
			break;
		case ExternalContentType.FurniturePack:
			FurniturePacksManager.DeleteFurniturePack(name);
			break;
		default:
			throw new InvalidOperationException(LanguageControl.Get(fName, 4));
		}
	}

	public static void ImportExternalContent(Stream stream, ExternalContentType type, string name, Action<string> success, Action<Exception> failure)
	{
		Task.Run(() =>
		{
			try
			{
				success(ImportExternalContentSync(stream, type, name));
			}
			catch (Exception obj)
			{
				failure(obj);
			}
		});
	}

	public static string ImportExternalContentSync(Stream stream, ExternalContentType type, string name)
	{
		return type switch
		{
			ExternalContentType.World => WorldsManager.ImportWorld(stream), 
			ExternalContentType.BlocksTexture => BlocksTexturesManager.ImportBlocksTexture(name, stream), 
			ExternalContentType.CharacterSkin => CharacterSkinsManager.ImportCharacterSkin(name, stream), 
			ExternalContentType.FurniturePack => FurniturePacksManager.ImportFurniturePack(name, stream), 
			_ => throw new InvalidOperationException(LanguageControl.Get(fName, 4)), 
		};
	}

	public static void ShowLoginUiIfNeeded(IExternalContentProvider provider, bool showWarningDialog, Action handler)
	{
		if (provider.RequiresLogin && !provider.IsLoggedIn)
		{
			Action loginAction = () =>
			{
				CancellableBusyDialog busyDialog = new CancellableBusyDialog(LanguageControl.Get(fName, 5), autoHideOnCancel: true);
				DialogsManager.ShowDialog(null, busyDialog);
				provider.Login(busyDialog.Progress, () =>
				{
					DialogsManager.HideDialog(busyDialog);
					handler?.Invoke();
				}, (Exception error) =>
				{
					DialogsManager.HideDialog(busyDialog);
					if (error != null)
					{
						DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get("Usual", "error"), error.Message, LanguageControl.Get("Usual", "ok"), null, null));
					}
				});
			};
			if (showWarningDialog)
			{
				DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 6), string.Format(LanguageControl.Get(fName, 7), provider.DisplayName), LanguageControl.Get(fName, 8), LanguageControl.Get("Usual", "cancel"), (MessageDialogButton b) =>
				{
					if (b == MessageDialogButton.Button1)
					{
						loginAction();
					}
				}));
			}
			else
			{
				loginAction();
			}
		}
		else
		{
			handler?.Invoke();
		}
	}

	public static void ShowUploadUi(ExternalContentType type, string name)
	{
		DialogsManager.ShowDialog(null, new SelectExternalContentProviderDialog(LanguageControl.Get(fName, 9), listingSupportRequired: false, (IExternalContentProvider provider) =>
		{
			try
			{
				if (provider != null)
				{
					ShowLoginUiIfNeeded(provider, showWarningDialog: true, () =>
					{
						CancellableBusyDialog busyDialog = new CancellableBusyDialog(LanguageControl.Get(fName, 10), autoHideOnCancel: false);
						DialogsManager.ShowDialog(null, busyDialog);
						Task.Run(() =>
						{
							bool needsDelete = false;
							string sourcePath = null;
							Stream stream = null;
							Action cleanup = () =>
							{
								Utilities.Dispose(ref stream);
								if (needsDelete && sourcePath != null)
								{
									try
									{
										Storage.DeleteFile(sourcePath);
									}
									catch
									{
									}
								}
							};
							try
							{
								string path;
								if (type == ExternalContentType.BlocksTexture)
								{
									sourcePath = BlocksTexturesManager.GetFileName(name);
									if (sourcePath == null)
									{
										throw new InvalidOperationException(LanguageControl.Get(fName, 11));
									}
									path = Storage.GetFileName(sourcePath);
								}
								else if (type == ExternalContentType.CharacterSkin)
								{
									if (CharacterSkinsManager.GetFileName(name, out sourcePath))
									{
										throw new InvalidOperationException(LanguageControl.Get(fName, 11));
									}
									path = Storage.GetFileName(sourcePath);
								}
								else if (type == ExternalContentType.FurniturePack)
								{
									sourcePath = FurniturePacksManager.GetFileName(name);
									if (sourcePath == null)
									{
										throw new InvalidOperationException(LanguageControl.Get(fName, 11));
									}
									path = Storage.GetFileName(sourcePath);
								}
								else
								{
									if (type != ExternalContentType.World)
									{
										throw new InvalidOperationException(LanguageControl.Get(fName, 12));
									}
									busyDialog.LargeMessage = LanguageControl.Get(fName, 13);
									sourcePath = "android:SurvivalCraft2.2/WorldUpload.tmp";
									needsDelete = true;
									path = WorldsManager.GetWorldInfo(name).WorldSettings.Name + ".scworld";
									using Stream targetStream = Storage.OpenFile(sourcePath, OpenFileMode.Create);
									WorldsManager.ExportWorld(name, targetStream);
								}
								busyDialog.LargeMessage = LanguageControl.Get(fName, 14);
								stream = Storage.OpenFile(sourcePath, OpenFileMode.Read);
								provider.Upload(path, stream, busyDialog.Progress, (string link) =>
								{
									long length = stream.Length;
									cleanup();
									DialogsManager.HideDialog(busyDialog);
									if (string.IsNullOrEmpty(link))
									{
										DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get("Usual", "success"), string.Format(LanguageControl.Get(fName, 15), DataSizeFormatter.Format(length)), LanguageControl.Get("Usual", "ok"), null, null));
									}
									else
									{
										DialogsManager.ShowDialog(null, new ExternalContentLinkDialog(link));
									}
								}, (Exception error) =>
								{
									cleanup();
									DialogsManager.HideDialog(busyDialog);
									DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get("Usual", "error"), error.Message, LanguageControl.Get("Usual", "ok"), null, null));
								});
							}
							catch (Exception ex2)
							{
								cleanup();
								DialogsManager.HideDialog(busyDialog);
								DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get("Usual", "error"), ex2.Message, LanguageControl.Get("Usual", "ok"), null, null));
							}
						});
					});
				}
			}
			catch (Exception ex)
			{
				DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get("Usual", "error"), ex.Message, LanguageControl.Get("Usual", "ok"), null, null));
			}
		}));
	}
}
