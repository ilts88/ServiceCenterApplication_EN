using Database;

namespace StringUtil
{
    static class StringValidator
    {
        static public bool IsValidNewRequest(Request request)                //checking the created repair request if it matches the expected format
        {
            if (request.fullname == null || request.fullname.Length > 50 || ContainsRestrictedSymbols(request.fullname))
                return false;
            else if (request.phone == null || request.phone.Length > 20 || ContainsRestrictedSymbols(request.phone))
                return false;
            else if (request.email == null || request.email.Length > 60 || ContainsRestrictedSymbols(request.email))
                return false;
            else if (request.model == null || request.model.Length > 15 || ContainsRestrictedSymbols(request.model))
                return false;
            else if (request.serial_number == null || request.serial_number.Length > 15 || ContainsRestrictedSymbols(request.serial_number))
                return false;
            else if (request.address == null || request.address.Length > 150 || ContainsRestrictedSymbols(request.address))
                return false;
            else if (request.malfunction == null || request.malfunction.Length > 1000 || ContainsRestrictedSymbols(request.malfunction))
                return false;
            else if (request.status != "processing")
                return false;
            else if (request.service_comment != null)
                return false;
            else
                return true;
            
        }

        static public bool ContainsRestrictedSymbols(string data)                 //check a string for characters that can be used for SQL injections and XSS attacks
        {
            if (data.Contains(";") || data.Contains("'") || data.Contains("--") || data.Contains("/*") || data.Contains("*/") || data.Contains("xp_") || data.Contains("<") || data.Contains(">") || data.Contains('"') || data.Contains("&"))
                return true;
            else
                return false;
        }

        static public bool IsValidStatus(string status)                 //checking the repair request status if it matches the expected format
        {
            if (status == "all" || status == "processing" || status == "appointed" || status == "rejected" || status == "resolved")
                return true;
            else
                return false;
        }

        static public bool IsValidService_comment(string service_comment)               //checking service center comment if it matches the expected format
        {
            if (service_comment == null || service_comment.Length > 350 || ContainsRestrictedSymbols(service_comment))
                return false;
            else
                return true;
        }

        static public bool IsValidArticleTitle(string title)               //checking the article title if it matches the expected format
        {
            if (title == null || title.Length > 80 || ContainsRestrictedSymbols(title))
                return false;
            else 
                return true;
        }

        static public bool IsValidArticleContent(string content)           //checking the article content if it matches the expected format
        {
            if (content == null || content.Length > 800 || ContainsRestrictedSymbols(content))
                return false;
            else 
                return true;
        }

        
    } 
}

static public class StringGenerator
{
    static private string? GeneratedKey = null;
    static private string GenerateKey(int minlength, int maxlength)                 //generate a random string for the authentication key whose length is within the range specified by the minimum and maximum length parameters
    {
        string symbols_for_generation = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";         //symbols used in generation
        string key = "";

        Random random = new Random();

        int length_of_key = random.Next(minlength, maxlength + 1);
        
        for (int i = 0; i < length_of_key; i++)
        {
            int randomindex = random.Next(0, symbols_for_generation.Length);
            key = key + symbols_for_generation[randomindex];
        }
        
        return key;
    }

    static public string GetGeneratedKey(int minlength,int maxlength)        //getting the generated key
    {
        if (GeneratedKey == null)                                       //if the key has not been generated previously, the function for its generation is called
        {
            GeneratedKey = GenerateKey(minlength, maxlength);
        }
        return GeneratedKey;
    }
}