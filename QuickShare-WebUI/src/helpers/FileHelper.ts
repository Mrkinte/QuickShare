export type FileProps = {
  name: string;
  fileId: number;
  extension: string;
};

export type FileType =
  | "folder"
  | "image"
  | "video"
  | "audio"
  | "document"
  | "compressed"
  | "file";

/**
 * 区分文件类型
 */
export const fileType = (extension: string): FileType => {
  // 文件夹类型
  if (extension === ".folder") {
    return "folder";
  }

  // 图片类型
  if (
    [".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg"].includes(
      extension,
    )
  ) {
    return "image";
  }

  // 视频类型
  if (
    [".mp4", ".webm", ".mov", ".avi", ".wmv", ".flv", ".mkv"].includes(
      extension,
    )
  ) {
    return "video";
  }

  // 音频类型
  if ([".mp3"].includes(extension)) {
    return "audio";
  }

  // 文档类型
  if (
    [
      ".txt",
      ".doc",
      ".docx",
      ".xls",
      ".xlsx",
      ".ppt",
      ".pptx",
      ".pdf",
      ".md",
    ].includes(extension)
  ) {
    return "document";
  }

  // 压缩文件类型
  if ([".zip", ".rar", ".7z"].includes(extension)) {
    return "compressed";
  }

  return "file";
};
