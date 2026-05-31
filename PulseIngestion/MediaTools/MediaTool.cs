using System.Threading.Tasks;

namespace PulseIngestion.MediaTools
{
	public abstract class MediaTool
	{
		protected PIConfig m_config;
		protected bool m_isReady = false;
		public MediaTool(PIConfig config)
		{
			m_config = config;
		}
		public bool IsReady()
		{
			return m_isReady;
		}
		public abstract Task ConvertToOutputFormat(string fileSource);
	}
}
