        public async Task<string> ReadKey()
        {
            // First try to read the perpetual key
            try
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(Globals.PerpetualLicenseFileName);
                return await FileIO.ReadTextAsync(file);
            }
            catch { }

            // Did not find perpetual key try the expiring universal key
            try
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(Globals.licenseFileName);
                return await FileIO.ReadTextAsync(file);
            }
            catch { }

            return "";
        }