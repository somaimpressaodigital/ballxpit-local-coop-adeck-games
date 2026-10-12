using System;
public static class CoopLocaleRules {
 public static bool UsesPortuguese(string code,string language){
  string normalized=(code??"").Trim().Replace('_','-').ToLowerInvariant();
  if(normalized.Length>0)return normalized=="pt"||normalized=="ptbr"||normalized=="ptpt"||normalized.StartsWith("pt-",StringComparison.Ordinal);
  string name=(language??"").Trim();
  return name.StartsWith("Portuguese",StringComparison.OrdinalIgnoreCase)||name.StartsWith("Português",StringComparison.OrdinalIgnoreCase)||name.StartsWith("Portugues",StringComparison.OrdinalIgnoreCase);
 }
}
